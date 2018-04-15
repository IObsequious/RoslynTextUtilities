// -----------------------------------------------------------------------
// <copyright file="TextUtilities.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;

namespace System.Text
{
    /// <summary>
    /// Holder for common Text Utility functions and values
    /// </summary>
    public static class TextUtilities
    {
        /// <summary>
        /// Windows-1252 Encoding
        /// </summary>
        public static Encoding Windows1252Encoding { get; } = Encoding.GetEncoding("windows-1252");

        public static byte[] ConvertToBytes(char[] charArray)
        {
            return ConvertToBytes(new string(charArray));
        }
        
        /// <summary>
        /// Converts a byte array into a string. Uses windows-1252 encoding.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ConvertToString(byte[] bytes)
        {
            return Windows1252Encoding.GetString(bytes);
        }

        public static byte[] ConvertToBytes(string text)
        {
            return Windows1252Encoding.GetBytes(text);
        }
        public static string GetValueText(string text)
        {
            string middle = GetMiddleText(text);
            if (middle.Length > 1)
            {
                if (middle.StartsWith("\\u", StringComparison.CurrentCultureIgnoreCase)
                    || middle.StartsWith("\\x", StringComparison.CurrentCultureIgnoreCase))
                {
                    string subText = middle.Substring(2);
                    int hex = int.Parse(subText, NumberStyles.HexNumber);
                    char ch = Convert.ToChar(hex);
                    return ch.ToString();
                }
            }

            return middle;
        }

        private static string GetMiddleText(string text)
        {
            if (StartsAndEndsWith(text, '\'', out int sEnd))
            {
                return text.Substring(1, sEnd);
            }

            return StartsAndEndsWith(text, '"', out int dEnd) ? text.Substring(1, dEnd) : text;
        }

        private static bool StartsAndEndsWith(string text, char ch, out int end)
        {
            end = text.Length - 1;
            char startChar = text[0];
            char endChar = text[end];
            end--;
            return ch == startChar && ch == endChar;
        }

        // Note: a small amount of this below logic is also inlined into SourceText.ParseLineBreaks
        // for performance reasons.
        public static int GetLengthOfLineBreak(SourceText text, int index)
        {
            var c = text[index];

            // common case - ASCII & not a line break
            // if (c > '\r' && c <= 127)
            // if (c >= ('\r'+1) && c <= 127)
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
                var next = index + 1;
                return next < text.Length && '\n' == text[next] ? 2 : 1;
            }

            if (IsAnyLineBreakCharacter(c))
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Return startLineBreak = index-1, lengthLineBreak = 2   if there is a \r\n at index-1
        /// Return startLineBreak = index,   lengthLineBreak = 1   if there is a 1-char newline at index
        /// Return startLineBreak = index+1, lengthLineBreak = 0   if there is no newline at index.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="startLinebreak"></param>
        /// <param name="lengthLinebreak"></param>
        public static void GetStartAndLengthOfLineBreakEndingAt(SourceText text, int index, out int startLinebreak, out int lengthLinebreak)
        {
            char c = text[index];
            if (c == '\n')
            {
                if (index > 0 && text[index - 1] == '\r')
                {
                    // "\r\n" is the only 2-character line break.
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

        /// <summary>
        /// Determine if the character in question is any line break character
        /// </summary>
        /// <param name="c"></param>
        public static bool IsAnyLineBreakCharacter(char c)
        {
            return c == '\n' || c == '\r' || c == '\u0085' || c == '\u2028' || c == '\u2029';
        }
    }
}
