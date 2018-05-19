using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    public static class Hash
    {
        public static int Combine(int newKey, int currentKey)
        {
            return unchecked(currentKey * (int) 0xA5555529 + newKey);
        }

        public static int Combine(bool newKeyPart, int currentKey)
        {
            return Combine(currentKey, newKeyPart ? 1 : 0);
        }

        public static int Combine<T>(T newKeyPart, int currentKey) where T : class
        {
            int hash = unchecked(currentKey * (int) 0xA5555529);
            if (newKeyPart != null)
            {
                return unchecked(hash + newKeyPart.GetHashCode());
            }

            return hash;
        }

        public static int CombineValues<T>(IEnumerable<T> values, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            int hashCode = 0;
            int count = 0;
            foreach (T value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                if (value != null)
                {
                    hashCode = Combine(value.GetHashCode(), hashCode);
                }
            }

            return hashCode;
        }

        public static int CombineValues<T>(T[] values, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            int maxSize = Math.Min(maxItemsToHash, values.Length);
            int hashCode = 0;
            for (int i = 0; i < maxSize; i++)
            {
                T value = values[i];
                if (value != null)
                {
                    hashCode = Combine(value.GetHashCode(), hashCode);
                }
            }

            return hashCode;
        }

        public static int CombineValues<T>(ImmutableArray<T> values, int maxItemsToHash = int.MaxValue)
        {
            if (values.IsDefaultOrEmpty)
            {
                return 0;
            }

            int hashCode = 0;
            int count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                if (value != null)
                {
                    hashCode = Combine(value.GetHashCode(), hashCode);
                }
            }

            return hashCode;
        }

        public static int CombineValues(IEnumerable<string> values, StringComparer stringComparer, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }

            int hashCode = 0;
            int count = 0;
            foreach (string value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                if (value != null)
                {
                    hashCode = Combine(stringComparer.GetHashCode(value), hashCode);
                }
            }

            return hashCode;
        }

        internal const int FnvOffsetBias = unchecked((int) 2166136261);
        internal const int FnvPrime = 16777619;

        public static int GetFNVHashCode(byte[] data)
        {
            int hashCode = FnvOffsetBias;
            for (int i = 0; i < data.Length; i++)
            {
                hashCode = unchecked((hashCode ^ data[i]) * FnvPrime);
            }

            return hashCode;
        }

        public static unsafe int GetFNVHashCode(byte* data, int length, out bool isAscii)
        {
            int hashCode = FnvOffsetBias;
            byte asciiMask = 0;
            for (int i = 0; i < length; i++)
            {
                byte b = data[i];
                asciiMask |= b;
                hashCode = unchecked((hashCode ^ b) * FnvPrime);
            }

            isAscii = (asciiMask & 0x80) == 0;
            return hashCode;
        }

        public static int GetFNVHashCode(ImmutableArray<byte> data)
        {
            int hashCode = FnvOffsetBias;
            for (int i = 0; i < data.Length; i++)
            {
                hashCode = unchecked((hashCode ^ data[i]) * FnvPrime);
            }

            return hashCode;
        }

        public static int GetFNVHashCode(string text, int start, int length)
        {
            int hashCode = FnvOffsetBias;
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                hashCode = unchecked((hashCode ^ text[i]) * FnvPrime);
            }

            return hashCode;
        }

        public static int GetCaseInsensitiveFNVHashCode(string text)
        {
            return GetCaseInsensitiveFNVHashCode(text, 0, text.Length);
        }

        public static int GetCaseInsensitiveFNVHashCode(string text, int start, int length)
        {
            int hashCode = FnvOffsetBias;
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                hashCode = unchecked((hashCode ^ CaseInsensitiveComparison.ToLower(text[i])) * FnvPrime);
            }

            return hashCode;
        }

        public static int GetFNVHashCode(string text, int start)
        {
            return GetFNVHashCode(text, start, text.Length - start);
        }

        public static int GetFNVHashCode(string text)
        {
            return CombineFNVHash(FnvOffsetBias, text);
        }

        public static int GetFNVHashCode(StringBuilder text)
        {
            int hashCode = FnvOffsetBias;
            int end = text.Length;
            for (int i = 0; i < end; i++)
            {
                hashCode = unchecked((hashCode ^ text[i]) * FnvPrime);
            }

            return hashCode;
        }

        public static int GetFNVHashCode(char[] text, int start, int length)
        {
            int hashCode = FnvOffsetBias;
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                hashCode = unchecked((hashCode ^ text[i]) * FnvPrime);
            }

            return hashCode;
        }

        public static int GetFNVHashCode(char ch)
        {
            return CombineFNVHash(FnvOffsetBias, ch);
        }

        public static int CombineFNVHash(int hashCode, string text)
        {
            foreach (char ch in text)
            {
                hashCode = unchecked((hashCode ^ ch) * FnvPrime);
            }

            return hashCode;
        }

        public static int CombineFNVHash(int hashCode, char ch)
        {
            return unchecked((hashCode ^ ch) * FnvPrime);
        }
    }
}
