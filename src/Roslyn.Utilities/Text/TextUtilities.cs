using System;

namespace Microsoft.CodeAnalysis.Text
{
    public static class TextUtilities
    {
        public static int GetLengthOfLineBreak(SourceText text, int index)
        {
            char c = text[index];
            const uint bias = '\r' + 1;
            if (unchecked(c - bias) <= 127 - bias)
            {
                return 0;
            }

            return GetLengthOfLineBreakSlow(text, index, c);
        }

        private static int GetLengthOfLineBreakSlow(SourceText text, int index, char c)
        {
            if (c == '\r')
            {
                int next = index + 1;
                return next < text.Length && '\n' == text[next] ? 2 : 1;
            }

            if (IsAnyLineBreakCharacter(c))
            {
                return 1;
            }

            return 0;
        }

        public static void GetStartAndLengthOfLineBreakEndingAt(SourceText text, int index, out int startLinebreak, out int lengthLinebreak)
        {
            char c = text[index];
            if (c == '\n')
            {
                if (index > 0 && text[index - 1] == '\r')
                {
                    startLinebreak = index - 1;
                    lengthLinebreak = 2;
                }
                else
                {
                    startLinebreak = index;
                    lengthLinebreak = 1;
                }
            }
            else if (IsAnyLineBreakCharacter(c))
            {
                startLinebreak = index;
                lengthLinebreak = 1;
            }
            else
            {
                startLinebreak = index + 1;
                lengthLinebreak = 0;
            }
        }

        public static bool IsAnyLineBreakCharacter(char c)
        {
            return c == '\n' || c == '\r' || c == '\u0085' || c == '\u2028' || c == '\u2029';
        }
    }
}
