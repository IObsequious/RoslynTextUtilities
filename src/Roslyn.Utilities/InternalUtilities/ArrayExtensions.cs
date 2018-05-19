using System;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    public static class ArrayExtensions
    {
        public static T[] Copy<T>(this T[] array, int start, int length)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(start <= array.Length);
            if (start + length > array.Length)
            {
                length = array.Length - start;
            }

            T[] newArray = new T[length];
            Array.Copy(array, start, newArray, 0, length);
            return newArray;
        }

        public static bool ValueEquals(this uint[] array, uint[] other)
        {
            if (array == other)
            {
                return true;
            }

            if (array == null || other == null || array.Length != other.Length)
            {
                return false;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static T[] InsertAt<T>(this T[] array, int position, T item)
        {
            T[] newArray = new T[array.Length + 1];
            if (position > 0)
            {
                Array.Copy(array, newArray, position);
            }

            if (position < array.Length)
            {
                Array.Copy(array, position, newArray, position + 1, array.Length - position);
            }

            newArray[position] = item;
            return newArray;
        }

        public static T[] Append<T>(this T[] array, T item)
        {
            return InsertAt(array, array.Length, item);
        }

        public static T[] InsertAt<T>(this T[] array, int position, T[] items)
        {
            T[] newArray = new T[array.Length + items.Length];
            if (position > 0)
            {
                Array.Copy(array, newArray, position);
            }

            if (position < array.Length)
            {
                Array.Copy(array, position, newArray, position + items.Length, array.Length - position);
            }

            items.CopyTo(newArray, position);
            return newArray;
        }

        public static T[] Append<T>(this T[] array, T[] items)
        {
            return InsertAt(array, array.Length, items);
        }

        public static T[] RemoveAt<T>(this T[] array, int position)
        {
            return RemoveAt(array, position, 1);
        }

        public static T[] RemoveAt<T>(this T[] array, int position, int length)
        {
            if (position + length > array.Length)
            {
                length = array.Length - position;
            }

            T[] newArray = new T[array.Length - length];
            if (position > 0)
            {
                Array.Copy(array, newArray, position);
            }

            if (position < newArray.Length)
            {
                Array.Copy(array, position + length, newArray, position, newArray.Length - position);
            }

            return newArray;
        }

        public static T[] ReplaceAt<T>(this T[] array, int position, T item)
        {
            T[] newArray = new T[array.Length];
            Array.Copy(array, newArray, array.Length);
            newArray[position] = item;
            return newArray;
        }

        public static T[] ReplaceAt<T>(this T[] array, int position, int length, T[] items)
        {
            return InsertAt(RemoveAt(array, position, length), position, items);
        }

        public static void ReverseContents<T>(this T[] array)
        {
            ReverseContents(array, 0, array.Length);
        }

        public static void ReverseContents<T>(this T[] array, int start, int count)
        {
            int end = start + count - 1;
            for (int i = start, j = end; i < j; i++, j--)
            {
                T tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }

        public static int BinarySearch(this int[] array, int value)
        {
            int low = 0;
            int high = array.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                int midValue = array[middle];
                if (midValue == value)
                {
                    return middle;
                }

                if (midValue > value)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return ~low;
        }

        public static int BinarySearchUpperBound(this int[] array, int value)
        {
            int low = 0;
            int high = array.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                if (array[middle] > value)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }
    }
}
