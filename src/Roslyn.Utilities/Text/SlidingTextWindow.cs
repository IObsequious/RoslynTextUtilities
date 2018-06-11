using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed class SlidingTextWindow : IDisposable
    {
        public const char InvalidCharacter = char.MaxValue;

        private const int DefaultWindowLength = 8192;
        private int _basis;
        private readonly int _textEnd;

        private readonly StringTable _strings;

        private static readonly ObjectPool<char[]> s_windowPool = new ObjectPool<char[]>(() => new char[DefaultWindowLength]);

        public SlidingTextWindow(SourceText text)
        {
            Text = text;
            _basis = 0;
            Offset = 0;
            _textEnd = text.Length;
            _strings = StringTable.GetInstance();
            CharacterWindow = s_windowPool.Allocate();
            LexemeRelativeStart = 0;
        }

        public void Dispose()
        {
            if (CharacterWindow != null)
            {
                s_windowPool.Free(CharacterWindow);
                CharacterWindow = null;
                _strings.Free();
            }
        }

        public SourceText Text { get; }

        public int Position
        {
            get
            {
                return _basis + Offset;
            }
        }

        public int Offset { get; private set; }

        public char[] Characters
        {
            get
            {
                return Text.ToCharArray();
            }
        }

        public char[] CharacterWindow { get; private set; }

        public int LexemeRelativeStart { get; private set; }

        public int CharacterWindowCount { get; private set; }

        public int LexemeStartPosition
        {
            get
            {
                return _basis + LexemeRelativeStart;
            }
        }

        public int Width
        {
            get
            {
                return Offset - LexemeRelativeStart;
            }
        }

        public void Start()
        {
            LexemeRelativeStart = Offset;
        }

        public void Reset(int position)
        {
            int relative = position - _basis;
            if (relative >= 0 && relative <= CharacterWindowCount)
            {
                Offset = relative;
            }
            else
            {
                int amountToRead = Math.Min(Text.Length, position + CharacterWindow.Length) - position;
                amountToRead = Math.Max(amountToRead, 0);
                if (amountToRead > 0)
                {
                    Text.CopyTo(position, CharacterWindow, 0, amountToRead);
                }

                LexemeRelativeStart = 0;
                Offset = 0;
                _basis = position;
                CharacterWindowCount = amountToRead;
            }
        }

        public bool MoreChars()
        {
            if (Offset >= CharacterWindowCount)
            {
                if (Position >= _textEnd)
                {
                    return false;
                }

                if (LexemeRelativeStart > CharacterWindowCount / 4)
                {
                    Array.Copy(CharacterWindow,
                        LexemeRelativeStart,
                        CharacterWindow,
                        0,
                        CharacterWindowCount - LexemeRelativeStart);
                    CharacterWindowCount -= LexemeRelativeStart;
                    Offset -= LexemeRelativeStart;
                    _basis += LexemeRelativeStart;
                    LexemeRelativeStart = 0;
                }

                if (CharacterWindowCount >= CharacterWindow.Length)
                {
                    char[] oldWindow = CharacterWindow;
                    char[] newWindow = new char[CharacterWindow.Length * 2];
                    Array.Copy(oldWindow, 0, newWindow, 0, CharacterWindowCount);
                    CharacterWindow = newWindow;
                }

                int amountToRead = Math.Min(_textEnd - (_basis + CharacterWindowCount),
                    CharacterWindow.Length - CharacterWindowCount);
                Text.CopyTo(_basis + CharacterWindowCount,
                    CharacterWindow,
                    CharacterWindowCount,
                    amountToRead);
                CharacterWindowCount += amountToRead;
                return amountToRead > 0;
            }

            return true;
        }

        public bool IsReallyAtEnd()
        {
            return Offset >= CharacterWindowCount && Position >= _textEnd;
        }

        public void AdvanceChar()
        {
            Offset++;
        }

        public void AdvanceChar(int n)
        {
            Offset += n;
        }

        public char NextChar()
        {
            char c = PeekChar();
            if (c != InvalidCharacter)
            {
                AdvanceChar();
            }
            return c;
        }

        public char PeekChar()
        {
            if (Offset >= CharacterWindowCount
                && !MoreChars())
            {
                return InvalidCharacter;
            }

            return CharacterWindow[Offset];
        }

        public char PeekChar(int delta)
        {
            int position = Position;
            AdvanceChar(delta);

            char ch;
            if (Offset >= CharacterWindowCount
                && !MoreChars())
            {
                ch = InvalidCharacter;
            }
            else
            {
                ch = CharacterWindow[Offset];
            }

            Reset(position);
            return ch;
        }

        public bool IsUnicodeEscape()
        {
            if (PeekChar() == '\\')
            {
                char ch2 = PeekChar(1);
                if (ch2 == 'U' || ch2 == 'u')
                {
                    return true;
                }
            }

            return false;
        }

        public bool AdvanceIfMatches(string desired)
        {
            int length = desired.Length;

            for (int i = 0; i < length; i++)
            {
                if (PeekChar(i) != desired[i])
                {
                    return false;
                }
            }

            AdvanceChar(length);
            return true;
        }

        public string Intern(StringBuilder text)
        {
            return _strings.Add(text);
        }

        public string Intern(char[] array, int start, int length)
        {
            return _strings.Add(array, start, length);
        }

        public string GetInternedText()
        {
            return Intern(CharacterWindow, LexemeRelativeStart, Width);
        }

        public string GetText(bool intern)
        {
            return GetText(LexemeStartPosition, Width, intern);
        }

        public string GetText(int position, int length, bool intern)
        {
            int offset = position - _basis;

            switch (length)
            {
                case 0:
                    return string.Empty;

                case 1:
                    if (CharacterWindow[offset] == ' ')
                    {
                        return " ";
                    }
                    if (CharacterWindow[offset] == '\n')
                    {
                        return "\n";
                    }
                    break;

                case 2:
                    char firstChar = CharacterWindow[offset];
                    if (firstChar == '\r' && CharacterWindow[offset + 1] == '\n')
                    {
                        return "\r\n";
                    }
                    if (firstChar == '/' && CharacterWindow[offset + 1] == '/')
                    {
                        return "//";
                    }
                    break;

                case 3:
                    if (CharacterWindow[offset] == '/' && CharacterWindow[offset + 1] == '/' && CharacterWindow[offset + 2] == ' ')
                    {
                        return "// ";
                    }
                    break;
            }

            if (intern)
            {
                return Intern(CharacterWindow, offset, length);
            }
            else
            {
                return new string(CharacterWindow, offset, length);
            }
        }

        public static char GetCharsFromUtf32(uint codepoint, out char lowSurrogate)
        {
            if (codepoint < (uint)0x00010000)
            {
                lowSurrogate = InvalidCharacter;
                return (char)codepoint;
            }
            else
            {
                Debug.Assert(codepoint > 0x0000FFFF && codepoint <= 0x0010FFFF);
                lowSurrogate = (char)(((codepoint - 0x00010000) % 0x0400) + 0xDC00);
                return (char)(((codepoint - 0x00010000) / 0x0400) + 0xD800);
            }
        }
    }
}
