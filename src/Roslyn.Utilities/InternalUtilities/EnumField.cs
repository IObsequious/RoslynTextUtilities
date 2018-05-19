using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    [DebuggerDisplay(value: "{GetDebuggerDisplay(), nq}")]
    internal struct EnumField
    {
        public static readonly IComparer<EnumField> Comparer = new EnumFieldComparer();
        public readonly string Name;
        public readonly ulong Value;
        public readonly object IdentityOpt;

        public EnumField(string name, ulong value, object identityOpt = null)
        {
            Debug.Assert(name != null);
            Name = name;
            Value = value;
            IdentityOpt = identityOpt;
        }

        public bool IsDefault
        {
            get
            {
                return Name == null;
            }
        }

        private string GetDebuggerDisplay()
        {
            return string.Format(format: "{{{0} = {1}}}", arg0: Name, arg1: Value);
        }

        public static EnumField FindValue(ArrayBuilder<EnumField> sortedFields, ulong value)
        {
            int start = 0;
            int end = sortedFields.Count;
            while (start < end)
            {
                int mid = start + (end - start) / 2;
                long diff = unchecked((long) value - (long) sortedFields[mid].Value);
                if (diff == 0)
                {
                    while (mid >= start && sortedFields[mid].Value == value)
                    {
                        mid--;
                    }

                    return sortedFields[mid + 1];
                }

                if (diff > 0)
                {
                    end = mid;
                }
                else
                {
                    start = mid + 1;
                }
            }

            return default(EnumField);
        }

        private class EnumFieldComparer : IComparer<EnumField>
        {
            int IComparer<EnumField>.Compare(EnumField field1, EnumField field2)
            {
                int diff = unchecked(((long) field2.Value).CompareTo((long) field1.Value));
                return diff == 0 ? string.CompareOrdinal(field1.Name, field2.Name) : diff;
            }
        }
    }
}
