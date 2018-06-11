using System;
using System.Collections;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    public static class SpecializedCollections
    {
        private static partial class Singleton
        {
            public class Enumerator<T> : IEnumerator<T>
            {
                private bool _moveNextCalled;

                public Enumerator(T value)
                {
                    Current = value;
                    _moveNextCalled = false;
                }

                public T Current { get; }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (!_moveNextCalled)
                    {
                        _moveNextCalled = true;
                        return true;
                    }

                    return false;
                }

                public void Reset()
                {
                    _moveNextCalled = false;
                }
            }
        }

        private static partial class Singleton
        {
            public sealed class List<T> : IList<T>, IReadOnlyCollection<T>
            {
                private readonly T _loneValue;

                public List(T value)
                {
                    _loneValue = value;
                }

                public void Add(T item)
                {
                    throw new NotSupportedException();
                }

                public void Clear()
                {
                    throw new NotSupportedException();
                }

                public bool Contains(T item)
                {
                    return EqualityComparer<T>.Default.Equals(_loneValue, item);
                }

                public void CopyTo(T[] array, int arrayIndex)
                {
                    array[arrayIndex] = _loneValue;
                }

                public int Count
                {
                    get
                    {
                        return 1;
                    }
                }

                public bool IsReadOnly
                {
                    get
                    {
                        return true;
                    }
                }

                public bool Remove(T item)
                {
                    throw new NotSupportedException();
                }

                public IEnumerator<T> GetEnumerator()
                {
                    return new Enumerator<T>(_loneValue);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                public T this[int index]
                {
                    get
                    {
                        if (index != 0)
                        {
                            throw new IndexOutOfRangeException();
                        }

                        return _loneValue;
                    }
                    set
                    {
                        throw new NotSupportedException();
                    }
                }

                public int IndexOf(T item)
                {
                    if (Equals(_loneValue, item))
                    {
                        return 0;
                    }

                    return -1;
                }

                public void Insert(int index, T item)
                {
                    throw new NotSupportedException();
                }

                public void RemoveAt(int index)
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static partial class ReadOnly
        {
            public class Set<TUnderlying, T> : Collection<TUnderlying, T>, ISet<T>, IReadOnlySet<T>
                where TUnderlying : ISet<T>
            {
                public Set(TUnderlying underlying)
                    : base(underlying)
                {
                }

                public new bool Add(T item)
                {
                    throw new NotSupportedException();
                }

                public void ExceptWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public void IntersectWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public bool IsProperSubsetOf(IEnumerable<T> other)
                {
                    return Underlying.IsProperSubsetOf(other);
                }

                public bool IsProperSupersetOf(IEnumerable<T> other)
                {
                    return Underlying.IsProperSupersetOf(other);
                }

                public bool IsSubsetOf(IEnumerable<T> other)
                {
                    return Underlying.IsSubsetOf(other);
                }

                public bool IsSupersetOf(IEnumerable<T> other)
                {
                    return Underlying.IsSupersetOf(other);
                }

                public bool Overlaps(IEnumerable<T> other)
                {
                    return Underlying.Overlaps(other);
                }

                public bool SetEquals(IEnumerable<T> other)
                {
                    return Underlying.SetEquals(other);
                }

                public void SymmetricExceptWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public void UnionWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static partial class ReadOnly
        {
            public class Enumerable<TUnderlying, T> : Enumerable<TUnderlying>, IEnumerable<T>
                where TUnderlying : IEnumerable<T>
            {
                public Enumerable(TUnderlying underlying)
                    : base(underlying)
                {
                }

                public new IEnumerator<T> GetEnumerator()
                {
                    return Underlying.GetEnumerator();
                }
            }
        }

        private static partial class ReadOnly
        {
            public class Enumerable<TUnderlying> : IEnumerable
                where TUnderlying : IEnumerable
            {
                protected readonly TUnderlying Underlying;

                public Enumerable(TUnderlying underlying)
                {
                    Underlying = underlying;
                }

                public IEnumerator GetEnumerator()
                {
                    return Underlying.GetEnumerator();
                }
            }
        }

        private static partial class ReadOnly
        {
            public class Collection<TUnderlying, T> : Enumerable<TUnderlying, T>, ICollection<T>
                where TUnderlying : ICollection<T>
            {
                public Collection(TUnderlying underlying)
                    : base(underlying)
                {
                }

                public void Add(T item)
                {
                    throw new NotSupportedException();
                }

                public void Clear()
                {
                    throw new NotSupportedException();
                }

                public bool Contains(T item)
                {
                    return Underlying.Contains(item);
                }

                public void CopyTo(T[] array, int arrayIndex)
                {
                    Underlying.CopyTo(array, arrayIndex);
                }

                public int Count
                {
                    get
                    {
                        return Underlying.Count;
                    }
                }

                public bool IsReadOnly
                {
                    get
                    {
                        return true;
                    }
                }

                public bool Remove(T item)
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static partial class Empty
        {
            public class Set<T> : Collection<T>, ISet<T>, IReadOnlySet<T>
            {
                public new static readonly Set<T> Instance = new Set<T>();

                protected Set()
                {
                }

                public new bool Add(T item)
                {
                    throw new NotSupportedException();
                }

                public void ExceptWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public void IntersectWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public bool IsProperSubsetOf(IEnumerable<T> other)
                {
                    return !other.IsEmpty();
                }

                public bool IsProperSupersetOf(IEnumerable<T> other)
                {
                    return false;
                }

                public bool IsSubsetOf(IEnumerable<T> other)
                {
                    return true;
                }

                public bool IsSupersetOf(IEnumerable<T> other)
                {
                    return other.IsEmpty();
                }

                public bool Overlaps(IEnumerable<T> other)
                {
                    return false;
                }

                public bool SetEquals(IEnumerable<T> other)
                {
                    return other.IsEmpty();
                }

                public void SymmetricExceptWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }

                public void UnionWith(IEnumerable<T> other)
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static partial class Empty
        {
            public class List<T> : Collection<T>, IList<T>, IReadOnlyList<T>
            {
                public new static readonly List<T> Instance = new List<T>();

                protected List()
                {
                }

                public int IndexOf(T item)
                {
                    return -1;
                }

                public void Insert(int index, T item)
                {
                    throw new NotSupportedException();
                }

                public void RemoveAt(int index)
                {
                    throw new NotSupportedException();
                }

                public T this[int index]
                {
                    get
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    set
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }

        private static partial class Empty
        {
            public class Enumerator<T> : Enumerator, IEnumerator<T>
            {
                public new static readonly IEnumerator<T> Instance = new Enumerator<T>();

                protected Enumerator()
                {
                }

                public new T Current
                {
                    get
                    {
                        throw new InvalidOperationException();
                    }
                }

                public void Dispose()
                {
                }
            }
        }

        private static partial class Empty
        {
            public class Enumerator : IEnumerator
            {
                public static readonly IEnumerator Instance = new Enumerator();

                protected Enumerator()
                {
                }

                public object Current
                {
                    get
                    {
                        throw new InvalidOperationException();
                    }
                }

                public bool MoveNext()
                {
                    return false;
                }

                public void Reset()
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static partial class Empty
        {
            public class Enumerable<T> : IEnumerable<T>
            {
                private readonly IEnumerator<T> _enumerator = Enumerator<T>.Instance;

                public IEnumerator<T> GetEnumerator()
                {
                    return _enumerator;
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
        }

        private static partial class Empty
        {
            public sealed class Dictionary<TKey, TValue> : Collection<KeyValuePair<TKey, TValue>>,
                IDictionary<TKey, TValue>,
                IReadOnlyDictionary<TKey, TValue>
            {
                public new static readonly Dictionary<TKey, TValue> Instance = new Dictionary<TKey, TValue>();

                private Dictionary()
                {
                }

                public void Add(TKey key, TValue value)
                {
                    throw new NotSupportedException();
                }

                public bool ContainsKey(TKey key)
                {
                    return false;
                }

                public ICollection<TKey> Keys
                {
                    get
                    {
                        return Collection<TKey>.Instance;
                    }
                }

                IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
                {
                    get
                    {
                        return Keys;
                    }
                }

                IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
                {
                    get
                    {
                        return Values;
                    }
                }

                public bool Remove(TKey key)
                {
                    throw new NotSupportedException();
                }

                public bool TryGetValue(TKey key, out TValue value)
                {
                    value = default;
                    return false;
                }

                public ICollection<TValue> Values
                {
                    get
                    {
                        return Collection<TValue>.Instance;
                    }
                }

                public TValue this[TKey key]
                {
                    get
                    {
                        throw new NotSupportedException();
                    }
                    set
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }

        private static partial class Empty
        {
        }

        private static partial class Empty
        {
            public class Collection<T> : Enumerable<T>, ICollection<T>
            {
                public static readonly ICollection<T> Instance = new Collection<T>();

                protected Collection()
                {
                }

                public void Add(T item)
                {
                    throw new NotSupportedException();
                }

                public void Clear()
                {
                    throw new NotSupportedException();
                }

                public bool Contains(T item)
                {
                    return false;
                }

                public void CopyTo(T[] array, int arrayIndex)
                {
                }

                public int Count
                {
                    get
                    {
                        return 0;
                    }
                }

                public bool IsReadOnly
                {
                    get
                    {
                        return true;
                    }
                }

                public bool Remove(T item)
                {
                    throw new NotSupportedException();
                }
            }
        }

        public static IEnumerator<T> EmptyEnumerator<T>()
        {
            return Empty.Enumerator<T>.Instance;
        }

        public static IEnumerable<T> EmptyEnumerable<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static ICollection<T> EmptyCollection<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static IList<T> EmptyList<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static IReadOnlyList<T> EmptyReadOnlyList<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static ISet<T> EmptySet<T>()
        {
            return Empty.Set<T>.Instance;
        }

        public static IReadOnlySet<T> EmptyReadOnlySet<T>()
        {
            return Empty.Set<T>.Instance;
        }

        public static IDictionary<TKey, TValue> EmptyDictionary<TKey, TValue>()
        {
            return Empty.Dictionary<TKey, TValue>.Instance;
        }

        public static IReadOnlyDictionary<TKey, TValue> EmptyReadOnlyDictionary<TKey, TValue>()
        {
            return Empty.Dictionary<TKey, TValue>.Instance;
        }

        public static IEnumerable<T> SingletonEnumerable<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static ICollection<T> SingletonCollection<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static IEnumerator<T> SingletonEnumerator<T>(T value)
        {
            return new Singleton.Enumerator<T>(value);
        }

        public static IList<T> SingletonList<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static IEnumerable<T> ReadOnlyEnumerable<T>(IEnumerable<T> values)
        {
            return new ReadOnly.Enumerable<IEnumerable<T>, T>(values);
        }

        public static ICollection<T> ReadOnlyCollection<T>(ICollection<T> collection)
        {
            return collection == null || collection.Count == 0 ?
                EmptyCollection<T>() :
                new ReadOnly.Collection<ICollection<T>, T>(collection);
        }

        public static ISet<T> ReadOnlySet<T>(ISet<T> set)
        {
            return set == null || set.Count == 0 ? EmptySet<T>() : new ReadOnly.Set<ISet<T>, T>(set);
        }

        public static IReadOnlySet<T> StronglyTypedReadOnlySet<T>(ISet<T> set)
        {
            return set == null || set.Count == 0 ? EmptyReadOnlySet<T>() : new ReadOnly.Set<ISet<T>, T>(set);
        }
    }
}
