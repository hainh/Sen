
/**************/
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.

// define TRACE_LEAKS to get additional diagnostics that can lead to the leak sources. note: it will
// make everything about 2-3x slower
// 
//#define TRACE_LEAKS

// define DETECT_LEAKS to detect possible leaks
//#if DEBUG
//#define DETECT_LEAKS  //for now always enable DETECT_LEAKS in debug.
//#endif

using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

#if DETECT_LEAKS
using System.Runtime.CompilerServices;
#endif

namespace Senla.Core./*Roslyn.*/Utilities
{
    /// <summary>
    /// Generic implementation of object pooling pattern with predefined pool size limit. The main
    /// purpose is that limited number of frequently used objects can be kept in the pool for
    /// further recycling.
    /// 
    /// Notes: 
    /// 1) it is not the goal to keep all returned objects. Pool is not meant for storage. If there
    ///    is no space in the pool, extra returned objects will be dropped.
    /// 
    /// 2) it is implied that if object was obtained from a pool, the caller will return it back in
    ///    a relatively short time. Keeping checked out objects for long durations is ok, but 
    ///    reduces usefulness of pooling. Just new up your own.
    /// 
    /// Not returning objects to the pool in not detrimental to the pool's work, but is a bad practice. 
    /// Rationale: 
    ///    If there is no intent for reusing the object, do not use pool - just use "new". 
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        [DebuggerDisplay("{Value,nq}")]
        protected struct Element
        {
            public T Value;
        }

        /// <remarks>
        /// Not using System.Func{T} because this file is linked into the (debugger) Formatter,
        /// which does not have that type (since it compiles against .NET 2.0).
        /// </remarks>
        public delegate T Factory();

        // Storage for the pool objects. The first item is stored in a dedicated field because we
        // expect to be able to satisfy most requests from it.
        protected T _firstItem;
        protected readonly Element[] _items;

        // factory is stored for the lifetime of the pool. We will call this only when pool needs to
        // expand. compared to "new T()", Func gives more flexibility to implementers and faster
        // than "new T()".
        protected readonly Factory _factory;

#if DETECT_LEAKS
        protected static readonly ConditionalWeakTable<T, LeakTracker> leakTrackers = new ConditionalWeakTable<T, LeakTracker>();

        protected class LeakTracker : IDisposable
        {
            protected volatile bool disposed;

#if TRACE_LEAKS
            public volatile object Trace = null;
#endif

            public void Dispose()
            {
                disposed = true;
                GC.SuppressFinalize(this);
            }

            protected string GetTrace()
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
        { }

        public ObjectPool(Factory factory, int size)
        {
            Debug.Assert(size >= 1);
            _factory = factory;
            _items = new Element[size - 1];
        }

        protected virtual T CreateInstance()
        {
            var inst = _factory();
            return inst;
        }

        /// <summary>
        /// Produces an instance.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search.
        /// </remarks>
        public T Allocate()
        {
            // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            // recently returned objects. Not a big deal.
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

        protected T AllocateSlow()
        {
            var items = _items;

            for (int i = 0; i < items.Length; i++)
            {
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
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

        /// <summary>
        /// Returns objects to the pool.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search in Allocate.
        /// </remarks>
        public void Free(T obj)
        {
            Validate(obj);
            ForgetTrackedObject(obj);

            if (_firstItem == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                _firstItem = obj;
            }
            else
            {
                FreeSlow(obj);
            }
        }

        protected void FreeSlow(T obj)
        {
            var items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Value == null)
                {
                    // Intentionally not using interlocked here. 
                    // In a worst case scenario two objects may be stored into same slot.
                    // It is very unlikely to happen and will only mean that one of the objects will get collected.
                    items[i].Value = obj;
                    break;
                }
            }
        }

        /// <summary>
        /// Removes an object from leak tracking.  
        /// 
        /// This is called when an object is returned to the pool.  It may also be explicitly 
        /// called if an object allocated from the pool is intentionally not being returned
        /// to the pool.  This can be of use with pooled arrays if the consumer wants to 
        /// return a larger array to the pool than was originally allocated.
        /// </summary>
        [Conditional("DEBUG")]
        public void ForgetTrackedObject(T old, T replacement = null)
        {
#if DETECT_LEAKS
            LeakTracker tracker;
            if (leakTrackers.TryGetValue(old, out tracker))
            {
                tracker.Dispose();
                leakTrackers.Remove(old);
            }
            else
            {
                var trace = CaptureStackTrace();
                Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nObject of type {typeof(T)} was freed, but was not from pool. \n Callstack: \n {trace} TRACEOBJECTPOOLLEAKS_END");
            }

            if (replacement != null)
            {
                tracker = new LeakTracker();
                leakTrackers.Add(replacement, tracker);
            }
#endif
        }

#if DETECT_LEAKS
        protected static Lazy<Type> _stackTraceType = new Lazy<Type>(() => Type.GetType("System.Diagnostics.StackTrace"));

        protected static object CaptureStackTrace()
        {
            return Activator.CreateInstance(_stackTraceType.Value);
        }
#endif

        [Conditional("DEBUG")]
        protected void Validate(object obj)
        {
            Debug.Assert(obj != null, "freeing null?");

            Debug.Assert(_firstItem != obj, "freeing twice?");

            var items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                var value = items[i].Value;
                if (value == null)
                {
                    return;
                }

                Debug.Assert(value != obj, "freeing twice?");
            }
        }
    }

    public class ArrayPool<T> where T : class
    {

        [DebuggerDisplay("{Value,nq}")]
        protected struct Element
        {
            public T Value;
        }

        protected class Element1
        {
            public T Value;
        }

        /// <remarks>
        /// Not using System.Func{T} because this file is linked into the (debugger) Formatter,
        /// which does not have that type (since it compiles against .NET 2.0).
        /// </remarks>
        public delegate T Factory(int length);

        // Storage for the pool objects. The first item is stored in a dedicated field because we
        // expect to be able to satisfy most requests from it.
        protected Dictionary<int, Element1> _firstItem = new Dictionary<int, Element1>(15);
        protected readonly Dictionary<int, Element[]> _items = new Dictionary<int, Element[]>(15);

        // factory is stored for the lifetime of the pool. We will call this only when pool needs to
        // expand. compared to "new T()", Func gives more flexibility to implementers and faster
        // than "new T()".
        protected readonly Factory _factory;

        // Size of each length pool
        protected readonly int _size;

#if DETECT_LEAKS
        protected static readonly ConditionalWeakTable<T, LeakTracker> leakTrackers = new ConditionalWeakTable<T, LeakTracker>();

        protected class LeakTracker : IDisposable
        {
            protected volatile bool disposed;

#if TRACE_LEAKS
            public volatile object Trace = null;
#endif

            public void Dispose()
            {
                disposed = true;
                GC.SuppressFinalize(this);
            }

            protected string GetTrace()
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

        public ArrayPool(Factory factory)
            : this(factory, Environment.ProcessorCount * 2)
        { }

        public ArrayPool(Factory factory, int size)
        {
            Debug.Assert(size >= 1);
            _factory = factory;
            _size = size;
        }

        protected virtual T CreateInstance(int size)
        {
            var inst = _factory(size);
            return inst;
        }

        /// <summary>
        /// Produces an instance.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search.
        /// </remarks>
        public T Allocate(int length)
        {
            Element1 firstItem;
            if (!_firstItem.TryGetValue(length, out firstItem))
            {
                firstItem = _firstItem[length] = new Element1();
                _items[length] = new Element[_size];
            }
            // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            // recently returned objects. Not a big deal.
            //T inst = _firstItem[length].Value;
            //Element elem = _firstItem[length];

            Element1 fstElem = _firstItem[length];
            T inst = fstElem.Value;
            if (inst == null || inst != Interlocked.CompareExchange(ref fstElem.Value, null, inst))
            {
                inst = AllocateSlow(length);
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

        protected T AllocateSlow(int length)
        {
            var items = _items[length];

            for (int i = 0; i < items.Length; i++)
            {
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                T inst = items[i].Value;
                if (inst != null)
                {
                    if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
                    {
                        return inst;
                    }
                }
            }

            return CreateInstance(length);
        }

        /// <summary>
        /// Returns objects to the pool.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search in Allocate.
        /// </remarks>
        public void Free(T obj, int length)
        {
            Element1 firstElem;
            if (!_firstItem.TryGetValue(length, out firstElem))
            {
                firstElem = new Element1();
                _firstItem[length] = firstElem;
                _items[length] = new Element[_size];
            }

            Validate(obj, length);
            ForgetTrackedObject(obj);

            if (_firstItem[length].Value == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                _firstItem[length].Value = obj;
            }
            else
            {
                FreeSlow(obj, length);
            }
        }

        protected void FreeSlow(T obj, int length)
        {
            var items = _items[length];
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Value == null)
                {
                    // Intentionally not using interlocked here. 
                    // In a worst case scenario two objects may be stored into same slot.
                    // It is very unlikely to happen and will only mean that one of the objects will get collected.
                    items[i].Value = obj;
                    break;
                }
            }
        }

        /// <summary>
        /// Removes an object from leak tracking.  
        /// 
        /// This is called when an object is returned to the pool.  It may also be explicitly 
        /// called if an object allocated from the pool is intentionally not being returned
        /// to the pool.  This can be of use with pooled arrays if the consumer wants to 
        /// return a larger array to the pool than was originally allocated.
        /// </summary>
        [Conditional("DEBUG")]
        public void ForgetTrackedObject(T old, T replacement = null)
        {
#if DETECT_LEAKS
            LeakTracker tracker;
            if (leakTrackers.TryGetValue(old, out tracker))
            {
                tracker.Dispose();
                leakTrackers.Remove(old);
            }
            else
            {
                var trace = CaptureStackTrace();
                Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nObject of type {typeof(T)} was freed, but was not from pool. \n Callstack: \n {trace} TRACEOBJECTPOOLLEAKS_END");
            }

            if (replacement != null)
            {
                tracker = new LeakTracker();
                leakTrackers.Add(replacement, tracker);
            }
#endif
        }

#if DETECT_LEAKS
        protected static Lazy<Type> _stackTraceType = new Lazy<Type>(() => Type.GetType("System.Diagnostics.StackTrace"));

        protected static object CaptureStackTrace()
        {
            return Activator.CreateInstance(_stackTraceType.Value);
        }
#endif

        [Conditional("DEBUG")]
        protected void Validate(object obj, int length)
        {
            Debug.Assert(obj != null, "freeing null?");

            Debug.Assert(_firstItem != obj, "freeing twice?");

            var items = _items[length];
            for (int i = 0; i < items.Length; i++)
            {
                var value = items[i].Value;
                if (value == null)
                {
                    return;
                }

                Debug.Assert(value != obj, "freeing twice?");
            }
        }
    }

    public static class ObjectPools
    {
    }

    public static class ArrayPools
    {
        /// <summary>
        /// A pool for byte array whom length is a power of two
        /// </summary>
        public static ArrayPool<byte[]> ByteArraysPOT = new ArrayPool<byte[]>(length => 
        {
            if (length > 0 && (length & (length - 1)) == 0)
                return new byte[length];
            throw new ArgumentException("Array length must be power of 2", "length");
        });

    }
}
