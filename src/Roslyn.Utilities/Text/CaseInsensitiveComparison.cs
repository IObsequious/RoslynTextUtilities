using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class CaseInsensitiveComparison
    {
        private static readonly TextInfo s_unicodeCultureTextInfo = GetUnicodeCulture().TextInfo;

        private static CultureInfo GetUnicodeCulture()
        {
            try
            {
                return new CultureInfo(name: "en");
            }
            catch (ArgumentException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        public static char ToLower(char c)
        {
            if (unchecked((uint) (c - 'A')) <= 'Z' - 'A')
            {
                return (char) (c | 0x20);
            }

            if (c < 0xC0)
            {
                return c;
            }

            return ToLowerNonAscii(c);
        }

        private static char ToLowerNonAscii(char c)
        {
            if (c == '\u0130')
            {
                return 'i';
            }

            return s_unicodeCultureTextInfo.ToLower(c);
        }

        private sealed class OneToOneUnicodeComparer : StringComparer
        {
            private static int CompareLowerUnicode(char c1, char c2)
            {
                return c1 == c2 ? 0 : ToLower(c1) - ToLower(c2);
            }

            public override int Compare(string str1, string str2)
            {
                if ((object) str1 == str2)
                {
                    return 0;
                }

                if ((object) str1 == null)
                {
                    return -1;
                }

                if ((object) str2 == null)
                {
                    return 1;
                }

                int len = Math.Min(str1.Length, str2.Length);
                for (int i = 0; i < len; i++)
                {
                    int ordDiff = CompareLowerUnicode(str1[i], str2[i]);
                    if (ordDiff != 0)
                    {
                        return ordDiff;
                    }
                }

                return str1.Length - str2.Length;
            }

            private static bool AreEqualLowerUnicode(char c1, char c2)
            {
                return c1 == c2 || ToLower(c1) == ToLower(c2);
            }

            public override bool Equals(string str1, string str2)
            {
                if ((object) str1 == str2)
                {
                    return true;
                }

                if ((object) str1 == null || (object) str2 == null)
                {
                    return false;
                }

                if (str1.Length != str2.Length)
                {
                    return false;
                }

                for (int i = 0; i < str1.Length; i++)
                {
                    if (!AreEqualLowerUnicode(str1[i], str2[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static bool EndsWith(string value, string possibleEnd)
            {
                if ((object) value == possibleEnd)
                {
                    return true;
                }

                if ((object) value == null || (object) possibleEnd == null)
                {
                    return false;
                }

                int i = value.Length - 1;
                int j = possibleEnd.Length - 1;
                if (i < j)
                {
                    return false;
                }

                while (j >= 0)
                {
                    if (!AreEqualLowerUnicode(value[i], possibleEnd[j]))
                    {
                        return false;
                    }

                    i--;
                    j--;
                }

                return true;
            }

            public static bool StartsWith(string value, string possibleStart)
            {
                if ((object) value == possibleStart)
                {
                    return true;
                }

                if ((object) value == null || (object) possibleStart == null)
                {
                    return false;
                }

                if (value.Length < possibleStart.Length)
                {
                    return false;
                }

                for (int i = 0; i < possibleStart.Length; i++)
                {
                    if (!AreEqualLowerUnicode(value[i], possibleStart[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode(string str)
            {
                int hashCode = Hash.FnvOffsetBias;
                for (int i = 0; i < str.Length; i++)
                {
                    hashCode = Hash.CombineFNVHash(hashCode, ToLower(str[i]));
                }

                return hashCode;
            }
        }

        private static readonly OneToOneUnicodeComparer s_comparer = new OneToOneUnicodeComparer();

        public static StringComparer Comparer
        {
            get
            {
                return s_comparer;
            }
        }

        public static bool Equals(string left, string right)
        {
            return s_comparer.Equals(left, right);
        }

        public static bool EndsWith(string value, string possibleEnd)
        {
            return OneToOneUnicodeComparer.EndsWith(value, possibleEnd);
        }

        public static bool StartsWith(string value, string possibleStart)
        {
            return OneToOneUnicodeComparer.StartsWith(value, possibleStart);
        }

        public static int Compare(string left, string right)
        {
            return s_comparer.Compare(left, right);
        }

        public static int GetHashCode(string value)
        {
            Debug.Assert(value != null);
            return s_comparer.GetHashCode(value);
        }

        public static string ToLower(string value)
        {
            if ((object) value == null)
            {
                return null;
            }

            if (value.Length == 0)
            {
                return value;
            }

            PooledStringBuilder pooledStrbuilder = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooledStrbuilder.Builder;
            builder.Append(value);
            ToLower(builder);
            return pooledStrbuilder.ToStringAndFree();
        }

        public static void ToLower(StringBuilder builder)
        {
            if (builder == null)
            {
                return;
            }

            for (int i = 0; i < builder.Length; i++)
            {
                builder[i] = ToLower(builder[i]);
            }
        }
    }
}
