using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    public sealed class JsonWriter : IDisposable
    {
        private readonly TextWriter _output;
        private int _indent;
        private Pending _pending;

        private enum Pending
        {
            None,
            NewLineAndIndent,
            CommaNewLineAndIndent
        }

        private const string Indentation = "  ";

        public JsonWriter(TextWriter output)
        {
            _output = output;
            _pending = Pending.None;
        }

        public void WriteObjectStart()
        {
            WriteStart('{');
        }

        public void WriteObjectStart(string key)
        {
            WriteKey(key);
            WriteObjectStart();
        }

        public void WriteObjectEnd()
        {
            WriteEnd('}');
        }

        public void WriteArrayStart()
        {
            WriteStart('[');
        }

        public void WriteArrayStart(string key)
        {
            WriteKey(key);
            WriteArrayStart();
        }

        public void WriteArrayEnd()
        {
            WriteEnd(']');
        }

        public void WriteKey(string key)
        {
            Write(key);
            _output.Write(value: ": ");
            _pending = Pending.None;
        }

        public void Write(string key, string value)
        {
            WriteKey(key);
            Write(value);
        }

        public void Write(string key, int value)
        {
            WriteKey(key);
            Write(value);
        }

        public void Write(string key, bool value)
        {
            WriteKey(key);
            Write(value);
        }

        public void Write(string value)
        {
            WritePending();
            _output.Write('"');
            _output.Write(EscapeString(value));
            _output.Write('"');
            _pending = Pending.CommaNewLineAndIndent;
        }

        public void Write(int value)
        {
            WritePending();
            _output.Write(value.ToString(CultureInfo.InvariantCulture));
            _pending = Pending.CommaNewLineAndIndent;
        }

        public void Write(bool value)
        {
            WritePending();
            _output.Write(value ? "true" : "false");
            _pending = Pending.CommaNewLineAndIndent;
        }

        private void WritePending()
        {
            if (_pending == Pending.None)
            {
                return;
            }

            Debug.Assert(_pending == Pending.NewLineAndIndent || _pending == Pending.CommaNewLineAndIndent);
            if (_pending == Pending.CommaNewLineAndIndent)
            {
                _output.Write(',');
            }

            _output.WriteLine();
            for (int i = 0; i < _indent; i++)
            {
                _output.Write(Indentation);
            }
        }

        private void WriteStart(char c)
        {
            WritePending();
            _output.Write(c);
            _pending = Pending.NewLineAndIndent;
            _indent++;
        }

        private void WriteEnd(char c)
        {
            _pending = Pending.NewLineAndIndent;
            _indent--;
            WritePending();
            _output.Write(c);
            _pending = Pending.CommaNewLineAndIndent;
        }

        public void Dispose()
        {
            _output.Dispose();
        }

        private static string EscapeString(string value)
        {
            PooledStringBuilder pooledBuilder = null;
            StringBuilder b = null;
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\"' || c == '\\' || ShouldAppendAsUnicode(c))
                {
                    if (b == null)
                    {
                        Debug.Assert(pooledBuilder == null);
                        pooledBuilder = PooledStringBuilder.GetInstance();
                        b = pooledBuilder.Builder;
                    }

                    if (count > 0)
                    {
                        b.Append(value, startIndex, count);
                    }

                    startIndex = i + 1;
                    count = 0;
                }

                switch (c)
                {
                    case '\"':
                        b.Append(value: "\\\"");
                        break;
                    case '\\':
                        b.Append(value: "\\\\");
                        break;
                    default:
                        if (ShouldAppendAsUnicode(c))
                        {
                            AppendCharAsUnicode(b, c);
                        }
                        else
                        {
                            count++;
                        }

                        break;
                }
            }

            if (b == null)
            {
                return value;
            }

            if (count > 0)
            {
                b.Append(value, startIndex, count);
            }

            return pooledBuilder.ToStringAndFree();
        }

        private static void AppendCharAsUnicode(StringBuilder builder, char c)
        {
            builder.Append(value: "\\u");
            builder.AppendFormat(CultureInfo.InvariantCulture, format: "{0:x4}", arg0: (int) c);
        }

        private static bool ShouldAppendAsUnicode(char c)
        {
            return c < ' ' ||
                   c >= (char) 0xfffe ||
                   c >= (char) 0xd800 && c <= (char) 0xdfff ||
                   c == '\u0085' ||
                   c == '\u2028' ||
                   c == '\u2029';
        }
    }
}
