﻿using System;
using System.Diagnostics;
using System.Threading;

#if DETECT_LEAKS
using System.Runtime.CompilerServices;

#endif

namespace Microsoft.CodeAnalysis.PooledObjects
{
    public class ObjectPool<T> where T : class
    {
        [DebuggerDisplay(value: "{" + nameof(Value) + ",nq}")]
        private struct Element
        {
            internal T Value;
        }

        public delegate T Factory();

        private T _firstItem;
        private readonly Element[] _items;
        private readonly Factory _factory;
#if DETECT_LEAKS
        private static readonly ConditionalWeakTable<T, LeakTracker> leakTrackers = new ConditionalWeakTable<T, LeakTracker>();

        private class LeakTracker : IDisposable
        {
            private volatile bool disposed;

#if TRACE_LEAKS
            internal volatile object Trace = null;
#endif

            public void Dispose()
            {
                disposed = true;
                GC.SuppressFinalize(this);
            }

            private string GetTrace()
            {
#if TRACE_LEAKS
                return Trace == null ? "" : Trace.ToString();
#else
                return "Leak tracing information is disabled. Define TRACE_LEAKS on ObjectPool`1.cs to get more info \n";
#endif
            }

            ~LeakTracker()
            {
                if (!this.disposed && !Environment.HasShutdownStarted)
                {
                    var trace = GetTrace();

                    // If you are seeing this message it means that object has been allocated from the pool 
                    // and has not been returned back. This is not critical, but turns pool into rather 
                    // inefficient kind of "new".
                    Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nPool detected potential leaking of {typeof(T)}. \n Location of the leak: \n {GetTrace()} TRACEOBJECTPOOLLEAKS_END");
                }
            }
        }
#endif

        public ObjectPool(Factory factory)
            : this(factory, Environment.ProcessorCount * 2)
        {
        }

        public ObjectPool(Factory factory, int size)
        {
            Debug.Assert(size >= 1);
            _factory = factory;
            _items = new Element[size - 1];
        }

        private T CreateInstance()
        {
            T inst = _factory();
            return inst;
        }

        public T Allocate()
        {
            T inst = _firstItem;
            if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
            {
                inst = AllocateSlow();
            }
#if DETECT_LEAKS
            var tracker = new LeakTracker();
            leakTrackers.Add(inst, tracker);

#if TRACE_LEAKS
            var frame = CaptureStackTrace();
            tracker.Trace = frame;
#endif
#endif
            return inst;
        }

        private T AllocateSlow()
        {
            Element[] items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                T inst = items[i].Value;
                if (inst != null)
                {
                    if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
                    {
                        return inst;
                    }
                }
            }

            return CreateInstance();
        }

        public void Free(T obj)
        {
            Validate(obj);
            if (_firstItem == null)
            {
                _firstItem = obj;
            }
            else
            {
                FreeSlow(obj);
            }
        }

        private void FreeSlow(T obj)
        {
            Element[] items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Value == null)
                {
                    items[i].Value = obj;
                    break;
                }
            }
        }

        [Conditional(conditionString: "DEBUG")]
        private void Validate(object obj)
        {
            Debug.Assert(obj != null, message: "freeing null?");
            Debug.Assert(_firstItem != obj, message: "freeing twice?");
            Element[] items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                T value = items[i].Value;
                if (value == null)
                {
                    return;
                }

                Debug.Assert(value != obj, message: "freeing twice?");
            }
        }
    }
}
