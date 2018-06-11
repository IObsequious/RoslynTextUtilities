using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace Roslyn.Utilities
{
    public static class HexUtilities
    {
        public static char HexDigit(int num)
        {
            return (char) (num < 10 ? num + '0' : num + ('A' - 10));
        }

        public static string EncodeHexString(byte[] sArray)
        {
            string result = null;
            if (sArray != null)
            {
                char[] hexOrder = new char[sArray.Length * 2];
                for (int i = 0, j = 0; i < sArray.Length; i++)
                {
                    int digit = (sArray[i] & 0xf0) >> 4;
                    hexOrder[j++] = HexDigit(digit);
                    digit = sArray[i] & 0x0f;
                    hexOrder[j++] = HexDigit(digit);
                }

                result = new string(hexOrder);
            }

            return result;
        }

        public static string EncodeHexStringFromInt(byte[] sArray)
        {
            string result = null;
            if (sArray != null)
            {
                char[] hexOrder = new char[sArray.Length * 2];
                int i = sArray.Length;
                int j = 0;
                while (i-- > 0)
                {
                    int digit = (sArray[i] & 0xf0) >> 4;
                    hexOrder[j++] = HexDigit(digit);
                    digit = sArray[i] & 0x0f;
                    hexOrder[j++] = HexDigit(digit);
                }

                result = new string(hexOrder);
            }

            return result;
        }

        public static int ConvertHexDigit(char val)
        {
            if (val <= '9' && val >= '0')
            {
                return val - '0';
            }

            if (val >= 'a' && val <= 'f')
            {
                return val - 'a' + 10;
            }

            if (val >= 'A' && val <= 'F')
            {
                return val - 'A' + 10;
            }

            throw new ArgumentOutOfRangeException(nameof(val));
        }

        public static byte[] DecodeHexString(string hexString)
        {
            if (hexString == null)
            {
                throw new ArgumentNullException(nameof(hexString));
            }

            Contract.EndContractBlock();
            bool spaceSkippingMode = false;
            int i = 0;
            int length = hexString.Length;
            if (length >= 2
                && hexString[0] == '0'
                && (hexString[1] == 'x' || hexString[1] == 'X'))
            {
                length = hexString.Length - 2;
                i = 2;
            }

            if (length % 2 != 0 && length % 3 != 2)
            {
                throw new ArgumentException(nameof(hexString));
            }

            byte[] sArray;
            if (length >= 3 && hexString[i + 2] == ' ')
            {
                spaceSkippingMode = true;

                sArray = new byte[(length / 3) + 1];
            }
            else
            {
                sArray = new byte[length / 2];
            }

            for (int j = 0; i < hexString.Length; i += 2, j++)
            {
                int rawdigit = ConvertHexDigit(hexString[i]);
                int digit = ConvertHexDigit(hexString[i + 1]);
                sArray[j] = (byte) (digit | (rawdigit << 4));
                if (spaceSkippingMode)
                {
                    i++;
                }
            }

            return sArray;
        }
    }

    public static class CharFacts
    {
                /// <summary>
        /// Returns true if the Unicode character is a hexadecimal digit.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns>true if the character is a hexadecimal digit 0-9, A-F, a-f.</returns>
        internal static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9')
                   || (c >= 'A' && c <= 'F')
                   || (c >= 'a' && c <= 'f');
        }

        /// <summary>
        /// Returns true if the Unicode character is a binary (0-1) digit.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns>true if the character is a binary digit.</returns>
        internal static bool IsBinaryDigit(char c)
        {
            return c == '0' | c == '1';
        }

        /// <summary>
        /// Returns true if the Unicode character is a decimal digit.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns>true if the Unicode character is a decimal digit.</returns>
        internal static bool IsDecDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Returns the value of a hexadecimal Unicode character.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        internal static int HexValue(char c)
        {
            Debug.Assert(IsHexDigit(c));
            return c >= '0' && c <= '9' ? c - '0' : (c & 0xdf) - 'A' + 10;
        }

        /// <summary>
        /// Returns the value of a binary Unicode character.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        internal static int BinaryValue(char c)
        {
            Debug.Assert(IsBinaryDigit(c));
            return c - '0';
        }

        /// <summary>
        /// Returns the value of a decimal Unicode character.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        internal static int DecValue(char c)
        {
            Debug.Assert(IsDecDigit(c));
            return c - '0';
        }

        // UnicodeCategory value | Unicode designation
        // -----------------------+-----------------------
        // UppercaseLetter         "Lu" (letter, uppercase)
        // LowercaseLetter         "Ll" (letter, lowercase)
        // TitlecaseLetter         "Lt" (letter, titlecase)
        // ModifierLetter          "Lm" (letter, modifier)
        // OtherLetter             "Lo" (letter, other)
        // NonSpacingMark          "Mn" (mark, nonspacing)
        // SpacingCombiningMark    "Mc" (mark, spacing combining)
        // EnclosingMark           "Me" (mark, enclosing)
        // DecimalDigitNumber      "Nd" (number, decimal digit)
        // LetterNumber            "Nl" (number, letter)
        // OtherNumber             "No" (number, other)
        // SpaceSeparator          "Zs" (separator, space)
        // LineSeparator           "Zl" (separator, line)
        // ParagraphSeparator      "Zp" (separator, paragraph)
        // Control                 "Cc" (other, control)
        // Format                  "Cf" (other, format)
        // Surrogate               "Cs" (other, surrogate)
        // PrivateUse              "Co" (other, private use)
        // ConnectorPunctuation    "Pc" (punctuation, connector)
        // DashPunctuation         "Pd" (punctuation, dash)
        // OpenPunctuation         "Ps" (punctuation, open)
        // ClosePunctuation        "Pe" (punctuation, close)
        // InitialQuotePunctuation "Pi" (punctuation, initial quote)
        // FinalQuotePunctuation   "Pf" (punctuation, final quote)
        // OtherPunctuation        "Po" (punctuation, other)
        // MathSymbol              "Sm" (symbol, math)
        // CurrencySymbol          "Sc" (symbol, currency)
        // ModifierSymbol          "Sk" (symbol, modifier)
        // OtherSymbol             "So" (symbol, other)
        // OtherNotAssigned        "Cn" (other, not assigned)

        /// <summary>
        /// Returns true if the Unicode character represents a whitespace.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsWhitespace(char ch)
        {
            // whitespace:
            //   Any character with Unicode class Zs
            //   Horizontal tab character (U+0009)
            //   Vertical tab character (U+000B)
            //   Form feed character (U+000C)

            // Space and no-break space are the only space separators (Zs) in ASCII range

            return ch == ' '
                || ch == '\t'
                || ch == '\v'
                || ch == '\f'
                || ch == '\u00A0' // NO-BREAK SPACE
                                  // The native compiler, in ScanToken, recognized both the byte-order
                                  // marker '\uFEFF' as well as ^Z '\u001A' as whitespace, although
                                  // this is not to spec since neither of these are in Zs. For the
                                  // sake of compatibility, we recognize them both here. Note: '\uFEFF'
                                  // also happens to be a formatting character (class Cf), which means
                                  // that it is a legal non-initial identifier character. So it's
                                  // especially funny, because it will be whitespace UNLESS we happen
                                  // to be scanning an identifier or keyword, in which case it winds
                                  // up in the identifier or keyword.
                || ch == '\uFEFF'
                || ch == '\u001A'
                || (ch > 255 && CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator);
        }

        /// <summary>
        /// Returns true if the Unicode character is a newline character.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsNewLine(char ch)
        {
            // new-line-character:
            //   Carriage return character (U+000D)
            //   Line feed character (U+000A)
            //   Next line character (U+0085)
            //   Line separator character (U+2028)
            //   Paragraph separator character (U+2029)

            return ch == '\r'
                || ch == '\n'
                || ch == '\u0085'
                || ch == '\u2028'
                || ch == '\u2029';
        }

        /// <summary>
        /// Returns true if the Unicode character can be the starting character of a C# identifier.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsIdentifierStartCharacter(char ch)
        {
            return UnicodeCharacterUtilities.IsIdentifierStartCharacter(ch);
        }

        /// <summary>
        /// Returns true if the Unicode character can be a part of a C# identifier.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsIdentifierPartCharacter(char ch)
        {
            return UnicodeCharacterUtilities.IsIdentifierPartCharacter(ch);
        }

        /// <summary>
        /// Check that the name is a valid identifier.
        /// </summary>
        /// <param name="name"></param>
        public static bool IsValidIdentifier(string name)
        {
            return UnicodeCharacterUtilities.IsValidIdentifier(name);
        }

        /// <summary>
        /// Spec section 2.4.2 says that identifiers are compared without regard
        /// to leading "@" characters or unicode formatting characters.  As in dev10,
        /// this is actually accomplished by dropping such characters during parsing.
        /// Unfortunately, metadata names can still contain these characters and will
        /// not be referenceable from source if they do (lookup will fail since the
        /// characters will have been dropped from the search string).
        /// See DevDiv #14432 for more.
        /// </summary>
        /// <param name="name"></param>
        internal static bool ContainsDroppedIdentifierCharacters(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            if (name[0] == '@')
            {
                return true;
            }

            int nameLength = name.Length;
            for (int i = 0; i < nameLength; i++)
            {
                if (UnicodeCharacterUtilities.IsFormattingChar(name[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsNonAsciiQuotationMark(char ch)
        {
            // CONSIDER: There are others:
            // http://en.wikipedia.org/wiki/Quotation_mark_glyphs#Quotation_marks_in_Unicode
            switch (ch)
            {
                case '\u2018': //LEFT SINGLE QUOTATION MARK
                case '\u2019': //RIGHT SINGLE QUOTATION MARK
                    return true;
                case '\u201C': //LEFT DOUBLE QUOTATION MARK
                case '\u201D': //RIGHT DOUBLE QUOTATION MARK
                    return true;
                default:
                    return false;
            }
        }
    }
}
