using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    public static class ObjectBinder
    {
        private static object s_gate = new object();
        private static ObjectBinderSnapshot? s_lastSnapshot = null;
        private static readonly Dictionary<Type, int> s_typeToIndex = new Dictionary<Type, int>();
        private static readonly List<Type> s_types = new List<Type>();
        private static readonly List<Func<ObjectReader, object>> s_typeReaders = new List<Func<ObjectReader, object>>();

        public static ObjectBinderSnapshot GetSnapshot()
        {
            lock (s_gate)
            {
                if (s_lastSnapshot == null)
                {
                    s_lastSnapshot = new ObjectBinderSnapshot(s_typeToIndex, s_types, s_typeReaders);
                }

                return s_lastSnapshot.Value;
            }
        }

        public static void RegisterTypeReader(Type type, Func<ObjectReader, object> typeReader)
        {
            lock (s_gate)
            {
                if (s_typeToIndex.ContainsKey(type))
                {
                    return;
                }

                int index = s_typeReaders.Count;
                s_types.Add(type);
                s_typeReaders.Add(typeReader);
                s_typeToIndex.Add(type, index);
                s_lastSnapshot = null;
            }
        }
    }
}
