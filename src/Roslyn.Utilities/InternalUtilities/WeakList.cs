using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    public sealed class WeakList<T> : IEnumerable<T>
        where T : class
    {
        private WeakReference<T>[] _items;
        private int _size;

        public WeakList()
        {
            _items = Array.Empty<WeakReference<T>>();
        }

        private void Resize()
        {
            Debug.Assert(_size == _items.Length);
            Debug.Assert(_items.Length == 0 || _items.Length >= MinimalNonEmptySize);
            int alive = _items.Length;
            int firstDead = -1;
            for (int i = 0; i < _items.Length; i++)
            {
                T target;
                if (!_items[i].TryGetTarget(out target))
                {
                    if (firstDead == -1)
                    {
                        firstDead = i;
                    }

                    alive--;
                }
            }

            if (alive < _items.Length / 4)
            {
                Shrink(firstDead, alive);
            }
            else if (alive >= 3 * _items.Length / 4)
            {
                WeakReference<T>[] newItems = new WeakReference<T>[GetExpandedSize(_items.Length)];
                if (firstDead >= 0)
                {
                    Compact(firstDead, newItems);
                }
                else
                {
                    Array.Copy(_items, 0, newItems, 0, _items.Length);
                    Debug.Assert(_size == _items.Length);
                }

                _items = newItems;
            }
            else
            {
                Compact(firstDead, _items);
            }

            Debug.Assert(_items.Length > 0 && _size < 3 * _items.Length / 4, "length: " + _items.Length + " size: " + _size);
        }

        private void Shrink(int firstDead, int alive)
        {
            int newSize = GetExpandedSize(alive);
            WeakReference<T>[] newItems = newSize == _items.Length ? _items : new WeakReference<T>[newSize];
            Compact(firstDead, newItems);
            _items = newItems;
        }

        private const int MinimalNonEmptySize = 4;

        private static int GetExpandedSize(int baseSize)
        {
            return Math.Max(baseSize * 2 + 1, MinimalNonEmptySize);
        }

        private void Compact(int firstDead, WeakReference<T>[] result)
        {
            Debug.Assert(_items[firstDead].IsNull());
            if (!ReferenceEquals(_items, result))
            {
                Array.Copy(_items, 0, result, 0, firstDead);
            }

            int oldSize = _size;
            int j = firstDead;
            for (int i = firstDead + 1; i < oldSize; i++)
            {
                WeakReference<T> item = _items[i];
                T target;
                if (item.TryGetTarget(out target))
                {
                    result[j++] = item;
                }
            }

            _size = j;
            if (ReferenceEquals(_items, result))
            {
                while (j < oldSize)
                {
                    _items[j++] = null;
                }
            }
        }

        public int WeakCount
        {
            get
            {
                return _size;
            }
        }

        public WeakReference<T> GetWeakReference(int index)
        {
            if (index < 0 || index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _items[index];
        }

        public void Add(T item)
        {
            if (_size == _items.Length)
            {
                Resize();
            }

            Debug.Assert(_size < _items.Length);
            _items[_size++] = new WeakReference<T>(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            int count = _size;
            int alive = _size;
            int firstDead = -1;
            for (int i = 0; i < count; i++)
            {
                T item;
                if (_items[i].TryGetTarget(out item))
                {
                    yield return item;
                }
                else
                {
                    if (firstDead < 0)
                    {
                        firstDead = i;
                    }

                    alive--;
                }
            }

            if (alive == 0)
            {
                _items = Array.Empty<WeakReference<T>>();
                _size = 0;
            }
            else if (alive < _items.Length / 4)
            {
                Shrink(firstDead, alive);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal WeakReference<T>[] TestOnly_UnderlyingArray
        {
            get
            {
                return _items;
            }
        }
    }
}
