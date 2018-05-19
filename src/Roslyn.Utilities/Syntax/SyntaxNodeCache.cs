﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using System.Diagnostics;
using Roslyn.Utilities;

#if STATS
using System.Threading;
#endif
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides caching functionality for green nonterminals with up to 3 children.
    /// Example:
    ///     When constructing a node with given kind, flags, child1 and child2, we can look up 
    ///     in the cache whether we already have a node that contains same kind, flags, 
    ///     child1 and child2 and use that.
    ///     
    ///     For the purpose of children comparison, reference equality is used as a much cheaper 
    ///     alternative to the structural/recursive equality. This implies that in order to de-duplicate
    ///     a node to a cache node, the children of two nodes must be already de-duplicated.     
    ///     When adding a node to the cache we verify that cache does contain node's children,
    ///     since otherwise there is no reason for the node to be used.
    ///     Tokens/nulls are for this purpose considered deduplicated. Indeed most of the tokens
    ///     are deduplicated via quick-scanner caching, so we just assume they all are.
    ///     
    ///     As a result of above, "fat" nodes with 4 or more children or their recursive parents
    ///     will never be in the cache. This naturally limits the typical single cache item to be 
    ///     a relatively simple expression. We do not want the cache to be completely unbounded 
    ///     on the item size. 
    ///     While it still may be possible to store a gigantic nested binary expression, 
    ///     it should be a rare occurrence.
    ///     
    ///     We only consider "normal" nodes to be cacheable. 
    ///     Nodes with diagnostics/annotations/directives/skipped, etc... have more complicated identity 
    ///     and are not likely to be repetitive.
    ///     
    /// </summary>
    internal class GreenStats
    {
        // TODO: remove when done tweaking this cache.
#if STATS
        private static GreenStats stats = new GreenStats();

        private int greenNodes;
        private int greenTokens;
        private int nontermsAdded;
        private int cacheableNodes;
        private int cacheHits;

        internal static void NoteGreen(GreenNode node)
        {
            Interlocked.Increment(ref stats.greenNodes);
            if (node.IsToken)
            {
                Interlocked.Increment(ref stats.greenTokens);
            }
        }

        internal static void ItemAdded()
        {
            Interlocked.Increment(ref stats.nontermsAdded);
        }
        
        internal static void ItemCacheable()
        {
            Interlocked.Increment(ref stats.cacheableNodes);
        }

        internal static void CacheHit()
        {
            Interlocked.Increment(ref stats.cacheHits);
        }

        ~GreenStats()
        {
            Console.WriteLine("Green: " + greenNodes);
            Console.WriteLine("GreenTk: " + greenTokens);
            Console.WriteLine("Nonterminals added: " + nontermsAdded);
            Console.WriteLine("Nonterminals cacheable: " + cacheableNodes);
            Console.WriteLine("CacheHits: " + cacheHits);
            Console.WriteLine("RateOfAll: " + (cacheHits * 100 / (cacheHits + greenNodes - greenTokens)) + "%");
            Console.WriteLine("RateOfCacheable: " + (cacheHits * 100 / (cacheableNodes)) + "%");
        }
#else
        internal static void NoteGreen(SyntaxNode node)
        {
        }

        [Conditional("DEBUG")]
        internal static void ItemAdded()
        {
        }

        [Conditional("DEBUG")]
        internal static void ItemCacheable()
        {
        }

        [Conditional("DEBUG")]
        internal static void CacheHit()
        {
        }
#endif
    }

    internal static class SyntaxNodeCache
    {
        private const int CacheSizeBits = 16;
        private const int CacheSize = 1 << CacheSizeBits;
        private const int CacheMask = CacheSize - 1;

        private struct Entry
        {
            public readonly int hash;
            public readonly SyntaxNode node;

            internal Entry(int hash, SyntaxNode node)
            {
                this.hash = hash;
                this.node = node;
            }
        }

        private static readonly Entry[] s_cache = new Entry[CacheSize];

        internal static void AddNode(SyntaxNode node, int hash)
        {
            if (AllChildrenInCache(node) && !node.IsMissing)
            {
                GreenStats.ItemAdded();

                Debug.Assert(node.GetCacheHash() == hash);

                var idx = hash & CacheMask;
                s_cache[idx] = new Entry(hash, node);
            }
        }

        private static bool CanBeCached(SyntaxNode child1)
        {
            return child1 == null || child1.IsCacheable;
        }

        private static bool CanBeCached(SyntaxNode child1, SyntaxNode child2)
        {
            return CanBeCached(child1) && CanBeCached(child2);
        }

        private static bool CanBeCached(SyntaxNode child1, SyntaxNode child2, SyntaxNode child3)
        {
            return CanBeCached(child1) && CanBeCached(child2) && CanBeCached(child3);
        }

        private static bool ChildInCache(SyntaxNode child)
        {
            // for the purpose of this function consider that 
            // null nodes, tokens and trivias are cached somewhere else.
            // TODO: should use slotCount
            if (child == null || child.SlotCount == 0) return true;

            int hash = child.GetCacheHash();
            int idx = hash & CacheMask;
            return s_cache[idx].node == child;
        }

        private static bool AllChildrenInCache(SyntaxNode node)
        {
            // TODO: should use slotCount
            var cnt = node.SlotCount;
            for (int i = 0; i < cnt; i++)
            {
                if (!ChildInCache((SyntaxNode)node.GetSlot(i)))
                {
                    return false;
                }
            }

            return true;
        }

        internal static SyntaxNode TryGetNode(int kind, SyntaxNode child1, out int hash)
        {
            return TryGetNode(kind, child1, GetDefaultNodeFlags(), out hash);
        }

        internal static SyntaxNode TryGetNode(int kind, SyntaxNode child1, NodeFlags flags, out int hash)
        {
            if (CanBeCached(child1))
            {
                GreenStats.ItemCacheable();

                int h = hash = GetCacheHash(kind, flags, child1);
                int idx = h & CacheMask;
                var e = s_cache[idx];
                if (e.hash == h && e.node != null && e.node.IsCacheEquivalent(kind, flags, child1))
                {
                    GreenStats.CacheHit();
                    return e.node;
                }
            }
            else
            {
                hash = -1;
            }

            return null;
        }

        internal static SyntaxNode TryGetNode(int kind, SyntaxNode child1, SyntaxNode child2, out int hash)
        {
            return TryGetNode(kind, child1, child2, GetDefaultNodeFlags(), out hash);
        }

        internal static SyntaxNode TryGetNode(int kind, SyntaxNode child1, SyntaxNode child2, NodeFlags flags, out int hash)
        {
            if (CanBeCached(child1, child2))
            {
                GreenStats.ItemCacheable();

                int h = hash = GetCacheHash(kind, flags, child1, child2);
                int idx = h & CacheMask;
                var e = s_cache[idx];
                if (e.hash == h && e.node != null && e.node.IsCacheEquivalent(kind, flags, child1, child2))
                {
                    GreenStats.CacheHit();
                    return e.node;
                }
            }
            else
            {
                hash = -1;
            }

            return null;
        }

        internal static SyntaxNode TryGetNode(int kind, SyntaxNode child1, SyntaxNode child2, SyntaxNode child3, out int hash)
        {
            return TryGetNode(kind, child1, child2, child3, GetDefaultNodeFlags(), out hash);
        }

        internal static SyntaxNode TryGetNode(int kind, SyntaxNode child1, SyntaxNode child2, SyntaxNode child3, NodeFlags flags, out int hash)
        {
            if (CanBeCached(child1, child2, child3))
            {
                GreenStats.ItemCacheable();

                int h = hash = GetCacheHash(kind, flags, child1, child2, child3);
                int idx = h & CacheMask;
                var e = s_cache[idx];
                if (e.hash == h && e.node != null && e.node.IsCacheEquivalent(kind, flags, child1, child2, child3))
                {
                    GreenStats.CacheHit();
                    return e.node;
                }
            }
            else
            {
                hash = -1;
            }

            return null;
        }

        public static NodeFlags GetDefaultNodeFlags()
        {
            return NodeFlags.IsNotMissing;
        }

        private static int GetCacheHash(int kind, NodeFlags flags, SyntaxNode child1)
        {
            int code = (int)(flags) ^ kind;
            // the only child is never null
            code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code);

            // ensure nonnegative hash
            return code & int.MaxValue;
        }

        private static int GetCacheHash(int kind, NodeFlags flags, SyntaxNode child1, SyntaxNode child2)
        {
            int code = (int)(flags) ^ kind;

            if (child1 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code);
            }
            if (child2 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child2), code);
            }

            // ensure nonnegative hash
            return code & int.MaxValue;
        }

        private static int GetCacheHash(int kind, NodeFlags flags, SyntaxNode child1, SyntaxNode child2, SyntaxNode child3)
        {
            int code = (int)(flags) ^ kind;

            if (child1 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code);
            }
            if (child2 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child2), code);
            }
            if (child3 != null)
            {
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child3), code);
            }

            // ensure nonnegative hash
            return code & int.MaxValue;
        }
    }
}
