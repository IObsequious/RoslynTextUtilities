using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    public static class ArrayBuilderExtensions
    {
        public static bool Any<T>(this ArrayBuilder<T> builder, Func<T, bool> predicate)
        {
            foreach (T item in builder)
            {
                if (predicate(item))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool All<T>(this ArrayBuilder<T> builder, Func<T, bool> predicate)
        {
            foreach (T item in builder)
            {
                if (!predicate(item))
                {
                    return false;
                }
            }

            return true;
        }

        public static ImmutableArray<TResult> SelectAsArray<TItem, TResult>(this ArrayBuilder<TItem> items, Func<TItem, TResult> map)
        {
            switch (items.Count)
            {
                case 0:
                    return ImmutableArray<TResult>.Empty;
                case 1:
                    return ImmutableArray.Create(map(items[0]));
                case 2:
                    return ImmutableArray.Create(map(items[0]), map(items[1]));
                case 3:
                    return ImmutableArray.Create(map(items[0]), map(items[1]), map(items[2]));
                case 4:
                    return ImmutableArray.Create(map(items[0]), map(items[1]), map(items[2]), map(items[3]));
                default:
                    ArrayBuilder<TResult> builder = ArrayBuilder<TResult>.GetInstance(items.Count);
                    foreach (TItem item in items)
                    {
                        builder.Add(map(item));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ArrayBuilder<TItem> items,
            Func<TItem, TArg, TResult> map,
            TArg arg)
        {
            switch (items.Count)
            {
                case 0:
                    return ImmutableArray<TResult>.Empty;
                case 1:
                    return ImmutableArray.Create(map(items[0], arg));
                case 2:
                    return ImmutableArray.Create(map(items[0], arg), map(items[1], arg));
                case 3:
                    return ImmutableArray.Create(map(items[0], arg), map(items[1], arg), map(items[2], arg));
                case 4:
                    return ImmutableArray.Create(map(items[0], arg), map(items[1], arg), map(items[2], arg), map(items[3], arg));
                default:
                    ArrayBuilder<TResult> builder = ArrayBuilder<TResult>.GetInstance(items.Count);
                    foreach (TItem item in items)
                    {
                        builder.Add(map(item, arg));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        public static void AddOptional<T>(this ArrayBuilder<T> builder, T item)
            where T : class
        {
            if (item != null)
            {
                builder.Add(item);
            }
        }

        public static void Push<T>(this ArrayBuilder<T> builder, T e)
        {
            builder.Add(e);
        }

        public static T Pop<T>(this ArrayBuilder<T> builder)
        {
            T e = builder.Peek();
            builder.RemoveAt(builder.Count - 1);
            return e;
        }

        public static T Peek<T>(this ArrayBuilder<T> builder)
        {
            return builder[builder.Count - 1];
        }

        public static ImmutableArray<T> ToImmutableOrEmptyAndFree<T>(this ArrayBuilder<T> builderOpt)
        {
            return builderOpt?.ToImmutableAndFree() ?? ImmutableArray<T>.Empty;
        }

        public static void AddIfNotNull<T>(this ArrayBuilder<T> builder, T? value)
            where T : struct
        {
            if (value != null)
            {
                builder.Add(value.Value);
            }
        }

        public static void AddIfNotNull<T>(this ArrayBuilder<T> builder, T value)
            where T : class
        {
            if (value != null)
            {
                builder.Add(value);
            }
        }
    }
}
