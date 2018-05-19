using System.Globalization;

namespace Roslyn.Utilities
{
    public static partial class UnicodeCharacterUtilities
    {
        public static bool IsIdentifierStartCharacter(char ch)
        {
            if (ch < 'a')
            {
                if (ch < 'A')
                {
                    return false;
                }

                return ch <= 'Z' || ch == '_';
            }

            if (ch <= 'z')
            {
                return true;
            }

            if (ch <= '\u007F')
            {
                return false;
            }

            return IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(ch));
        }

        public static bool IsIdentifierPartCharacter(char ch)
        {
            if (ch < 'a')
            {
                if (ch < 'A')
                {
                    return ch >= '0' && ch <= '9';
                }

                return ch <= 'Z' || ch == '_';
            }

            if (ch <= 'z')
            {
                return true;
            }

            if (ch <= '\u007F')
            {
                return false;
            }

            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            return IsLetterChar(cat) || IsDecimalDigitChar(cat) || IsConnectingChar(cat) || IsCombiningChar(cat) || IsFormattingChar(cat);
        }

        public static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!IsIdentifierStartCharacter(name[0]))
            {
                return false;
            }

            int nameLength = name.Length;
            for (int i = 1; i < nameLength; i++)
            {
                if (!IsIdentifierPartCharacter(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLetterChar(UnicodeCategory cat)
        {
            switch (cat)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
            }

            return false;
        }

        private static bool IsCombiningChar(UnicodeCategory cat)
        {
            switch (cat)
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                    return true;
            }

            return false;
        }

        private static bool IsDecimalDigitChar(UnicodeCategory cat)
        {
            return cat == UnicodeCategory.DecimalDigitNumber;
        }

        private static bool IsConnectingChar(UnicodeCategory cat)
        {
            return cat == UnicodeCategory.ConnectorPunctuation;
        }

        public static bool IsFormattingChar(char ch)
        {
            return ch > 127 && IsFormattingChar(CharUnicodeInfo.GetUnicodeCategory(ch));
        }

        private static bool IsFormattingChar(UnicodeCategory cat)
        {
            return cat == UnicodeCategory.Format;
        }
    }
}
