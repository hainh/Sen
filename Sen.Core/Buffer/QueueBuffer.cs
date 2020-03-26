using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Senla.Core.Buffer
{
    /// <summary>Represents a first-in, first-out collection of objects.</summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    /// <filterpriority>1</filterpriority>
    [ComVisible(false)]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class QueueBuffer<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
    {
        protected T[] _array;

        protected int _head;

        protected int _tail;

        protected int _size;

        protected int _version;

        [NonSerialized]
        private object _syncRoot;

        protected const int _MinimumGrow = 512;

        protected const int _ShrinkThreshold = 128;

        protected const int _GrowFactor = 200;

        protected const int _DefaultCapacity = 512;

        private static Utilities.ArrayPool<T[]> _arrayPoolPot;

        public static Utilities.ArrayPool<T[]> ArrayPoolPOT { get { return _arrayPoolPot; } }

        /// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.Generic.Queue`1" />.</summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.Queue`1" />.</returns>
        public int Count
        {
            get
            {
                return this._size;
            }
        }

        /// <summary>Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).</summary>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise, false.  In the default implementation of <see cref="T:System.Collections.Generic.Queue`1" />, this property always returns false.</returns>

        bool System.Collections.ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.  In the default implementation of <see cref="T:System.Collections.Generic.Queue`1" />, this property always returns the current instance.</returns>

        object System.Collections.ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
                }
                return this._syncRoot;
            }
        }

        static QueueBuffer()
        {
            _arrayPoolPot = new Utilities.ArrayPool<T[]>(length =>
            {
                if (length > 0 && (length & (length - 1)) == 0)
                    return new T[length];
                throw new ArgumentException("Array length must be power of 2", "length");
            });
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.Queue`1" /> class that is empty and has the default initial capacity.</summary>
        public QueueBuffer()
        {
            this._array = AllocateArray(1);
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.Queue`1" /> class that is empty and has the specified initial capacity.</summary>
        /// <param name="capacity">The initial number of elements that the <see cref="T:System.Collections.Generic.Queue`1" /> can contain.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="capacity" /> is less than zero.</exception>
        public QueueBuffer(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", "Need non neg num required");
            }
            this._array = AllocateArray(capacity);
            this._head = 0;
            this._tail = 0;
            this._size = 0;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.Queue`1" /> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.</summary>
        /// <param name="collection">The collection whose elements are copied to the new <see cref="T:System.Collections.Generic.Queue`1" />.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="collection" /> is null.</exception>
        public QueueBuffer(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection", "Need non nul");
            }
            this._array = AllocateArray(Math.Max(_DefaultCapacity, collection.Count()));
            this._size = 0;
            this._version = 0;
            foreach (T t in collection)
            {
                this.Enqueue(t);
            }
        }

        /// <summary>Removes all objects from the <see cref="T:System.Collections.Generic.Queue`1" />.</summary>
        /// <filterpriority>1</filterpriority>
        public void Clear()
        {
            if (this._head >= this._tail)
            {
                Array.Clear(this._array, this._head, (int)this._array.Length - this._head);
                Array.Clear(this._array, 0, this._tail);
            }
            else
            {
                Array.Clear(this._array, this._head, this._size);
            }
            this._head = 0;
            this._tail = 0;
            this._size = 0;
            this._version = this._version + 1;
        }

        /// <summary>
        /// Remove all objects from the <see cref="T:System.Collections.Generic.Queue`1" /> and set head to arbitary index.
        /// </summary>
        /// <param name="headStart"></param>
        public void Clear(int headStart)
        {
            Clear();
            this._head = headStart;
            this._tail = headStart;
        }

        /// <summary>Determines whether an element is in the <see cref="T:System.Collections.Generic.Queue`1" />.</summary>
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.Queue`1" />; otherwise, false.</returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.Queue`1" />. The value can be null for reference types.</param>
        public bool Contains(T item)
        {
            int length = this._head;
            int num = this._size;
            EqualityComparer<T> @default = EqualityComparer<T>.Default;
            while (true)
            {
                int num1 = num;
                num = num1 - 1;
                if (num1 <= 0)
                {
                    break;
                }
                if (item == null)
                {
                    if (this._array[length] == null)
                    {
                        return true;
                    }
                }
                else if (this._array[length] != null && @default.Equals(this._array[length], item))
                {
                    return true;
                }
                length = (length + 1) % (int)this._array.Length;
            }
            return false;
        }

        /// <summary>Copies the <see cref="T:System.Collections.Generic.QueueBuffer&lt;T&gt;" /> elements to an existing one-dimensional <see cref="T:System.Array" />, starting at the specified array index.</summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.Queue`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="array" /> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="arrayIndex" /> is less than zero.</exception>
        /// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.Queue`1" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0 || arrayIndex > (int)array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }
            int length = (int)array.Length;
            if (length - arrayIndex < this._size)
            {
                throw new ArgumentException("Invalid off len");
            }
            int num = (length - arrayIndex < this._size ? length - arrayIndex : this._size);
            if (num == 0)
            {
                return;
            }
            int num1 = ((int)this._array.Length - this._head < num ? (int)this._array.Length - this._head : num);
            Array.Copy(this._array, this._head, array, arrayIndex, num1);
            num = num - num1;
            if (num > 0)
            {
                Array.Copy(this._array, 0, array, arrayIndex + (int)this._array.Length - this._head, num);
            }
        }

        /// <summary>
        /// Copies the <see cref="QueueBuffer{T}"/> elements to an existing
        /// one-dimensional <see cref="T:System.Array" />, starting at the specified array index. If the <see cref="T:System.Array" /> 
        /// is not enough space or the <see cref="QueueBuffer{T}"/> is out of elements then copying stops.
        /// </summary>
        /// <param name="array">Destination array</param>
        /// <param name="arrayIndex">Start index in the array</param>
        /// <param name="offset">Offset in the <see cref="QueueBuffer{T}"/> from where copying starts</param>
        /// <param name="count">Number of elements to copy</param>
        /// <returns>Number of element copied</returns>
        public int CopyTo(T[] array, int arrayIndex, int offset, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0 || arrayIndex > (int)array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }
            int length = (int)array.Length;
            //if (length - arrayIndex < this._size)
            //{
            //    throw new ArgumentException("Invalid off len");
            //}
            if (this._size == 0)
            {
                return 0;
            }
            count = Math.Min(count, length - arrayIndex);
            count = Math.Min(count, this._size - offset);

            int num = count;
            int num1 = ((int)this._array.Length - this._head - offset < num ? (int)this._array.Length - this._head - offset : num);
            Array.Copy(this._array, this._head + offset, array, arrayIndex, num1);
            num = num - num1;
            if (num > 0)
            {
                Array.Copy(this._array, 0, array, arrayIndex + num1, num);
            }

            return count;
        }

        /// <summary>Removes and returns the object at the beginning of the <see cref="T:System.Collections.Generic.Queue`1" />.</summary>
        /// <returns>The object that is removed from the beginning of the <see cref="T:System.Collections.Generic.Queue`1" />.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Collections.Generic.Queue`1" /> is empty.</exception>
        public T Dequeue()
        {
            if (this._size == 0)
            {
                throw new InvalidOperationException("Empty queue");
            }
            T t = this._array[this._head];
            this._array[this._head] = default(T);
            this._head = (this._head + 1) % (int)this._array.Length;
            this._size = this._size - 1;
            this._version = this._version + 1;
            return t;
        }

        /// <summary>
        /// Discard N objects at head of this <see cref="QueueBuffer{T}"/>
        /// </summary>
        /// <param name="count"></param>
        public void DiscardDequeu(int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (this._size <= count)
            {
                Clear(0);
                return;
            }

            int continuousItemsCount = this._array.Length - this._head;
            if (count <= continuousItemsCount) // and _size > count so _head < _tail
            {
                Array.Clear(this._array, this._head, count);
            }
            else // _head > _tail
            {
                Array.Clear(this._array, this._head, continuousItemsCount);
                Array.Clear(this._array, 0, count - continuousItemsCount);
            }

            this._head = (this._head + count) % (int)this._array.Length;
            this._size = this._size - count;
            this._version = this._version + 1;
        }

        /// <summary>Adds an object to the end of the <see cref="T:System.Collections.Generic.Queue`1" />.</summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.Queue`1" />. The value can be null for reference types.</param>
        public void Enqueue(T item)
        {
            if (this._size == (int)this._array.Length)
            {
                int length = (int)((long)((int)this._array.Length) * (long)_GrowFactor / (long)100);
                if (length < this._array.Length + _MinimumGrow)
                {
                    length = this._array.Length + _MinimumGrow;
                }
                this.SetCapacity(length);
            }
            this._array[this._tail] = item;
            this._tail = (this._tail + 1) % (int)this._array.Length;
            this._size = this._size + 1;
            this._version = this._version + 1;
        }

        public void Enqueue(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Enqueue(item);
            }
        }

        public void Enqueue(T[] items)
        {
            Enqueue(items, 0, items.Length);
        }

        public void Enqueue(T[] items, int start, int count)
        {
            if (start < 0 || count < 0 || start + count > items.Length)
            {
                throw new IndexOutOfRangeException();
            }
            if (this._size + count > this._array.Length)
            {
                int length = (int)((long)((int)this._array.Length) * (long)_GrowFactor / (long)100);
                if (length < this._array.Length + _MinimumGrow)
                {
                    length = this._array.Length + _MinimumGrow;
                }

                if (this._size + count > length)
                {
                    length = this._size + count * 2;
                }
                this.SetCapacity(length);
            }

            if (this._tail + count < this._array.Length)
            {
                Array.Copy(items, start, this._array, this._tail, count);
                this._tail = (this._tail + count) % this._array.Length;
            }
            else
            {
                int num1 = this._array.Length - this._tail;
                Array.Copy(items, start, this._array, this._tail, num1);
                var num2 = count - num1;
                Array.Copy(items, start + num1, this._array, 0, num2);
                this._tail = num2;
            }

            this._size = this._size + count;
            this._version = this._version + 1;
        }

        internal T GetElement(int i)
        {
            return this._array[(this._head + i) % (int)this._array.Length];
        }

        /// <summary>
        /// Get object at a specific index
        /// </summary>
        /// <param name="index">Index in queue</param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                if (index >= this._size)
                {
                    throw new IndexOutOfRangeException();
                }
                return GetElement(index);
            }
        }

        /// <summary>
        /// Make this queue head start at index of 0 in the underly data array
        /// </summary>
        public void Heapyfy()
        {
            if (this._head == 0)
            {
                return;
            }
            SetCapacity(_array.Length);
        }

        public T[] UnderlyArray
        {
            get
            {
                return _array;
            }
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="T:System.Collections.Generic.Queue`1" />.</summary>
        /// <returns>An <see cref="T:System.Collections.Generic.Queue`1.Enumerator" /> for the <see cref="T:System.Collections.Generic.Queue`1" />.</returns>
        public QueueBuffer<T>.Enumerator GetEnumerator()
        {
            return new QueueBuffer<T>.Enumerator(this);
        }

        /// <summary>Returns the object at the beginning of the <see cref="T:System.Collections.Generic.Queue`1" /> without removing it.</summary>
        /// <returns>The object at the beginning of the <see cref="T:System.Collections.Generic.Queue`1" />.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Collections.Generic.Queue`1" /> is empty.</exception>
        public T Peek()
        {
            if (this._size == 0)
            {
                throw new InvalidOperationException("Empty queue");
            }
            return this._array[this._head];
        }

        protected void SetCapacity(int capacity)
        {
            T[] tArray = AllocateArray(capacity);
            if (this._size > 0)
            {
                if (this._head >= this._tail)
                {
                    Array.Copy(this._array, this._head, tArray, 0, (int)this._array.Length - this._head);
                    Array.Copy(this._array, 0, tArray, (int)this._array.Length - this._head, this._tail);
                }
                else
                {
                    Array.Copy(this._array, this._head, tArray, 0, this._size);
                }
            }
            _arrayPoolPot.Free(this._array, this._array.Length);
            this._array = tArray;
            this._head = 0;
            this._tail = (this._size == capacity ? 0 : this._size);
            this._version = this._version + 1;
        }

        protected static T[] AllocateArray(int v)
        {
            // Find next power of two
            // See http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return _arrayPoolPot.Allocate(v);
        }

        IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            return new QueueBuffer<T>.Enumerator(this);
        }

        /// <summary>Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.</summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="array" /> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="index" /> is less than zero.</exception>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="array" /> is multidimensional.-or-<paramref name="array" /> does not have zero-based indexing.-or-The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />.-or-The type of the source <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination <paramref name="array" />.</exception>

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (array.Rank != 1)
            {
                throw new ArgumentException("Multi dimension not supported");
            }
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("Non zero lower bound");
            }
            int length = array.Length;
            if (index < 0 || index > length)
            {
                throw new ArgumentOutOfRangeException("index", "Index out of bound");
            }
            if (length - index < this._size)
            {
                throw new ArgumentException("Invalid offset length");
            }
            int num = (length - index < this._size ? length - index : this._size);
            if (num == 0)
            {
                return;
            }
            try
            {
                int num1 = ((int)this._array.Length - this._head < num ? (int)this._array.Length - this._head : num);
                Array.Copy(this._array, this._head, array, index, num1);
                num = num - num1;
                if (num > 0)
                {
                    Array.Copy(this._array, 0, array, index + (int)this._array.Length - this._head, num);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Invalid array type");
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> that can be used to iterate through the collection.</returns>
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new QueueBuffer<T>.Enumerator(this);
        }

        /// <summary>Copies the <see cref="T:System.Collections.Generic.Queue`1" /> elements to a new array.
        /// Call <see cref="QueueBuffer{T}.ArrayPoolPOT.Free(T, int)"/> on returned array as you done with it</summary>
        /// <returns>A new array containing elements copied from the <see cref="T:System.Collections.Generic.Queue`1" />.</returns>
        public T[] ToArray()
        {
            T[] tArray = new T[this._size];
            if (this._size == 0)
            {
                return tArray;
            }
            if (this._head >= this._tail)
            {
                Array.Copy(this._array, this._head, tArray, 0, (int)this._array.Length - this._head);
                Array.Copy(this._array, 0, tArray, (int)this._array.Length - this._head, this._tail);
            }
            else
            {
                Array.Copy(this._array, this._head, tArray, 0, this._size);
            }
            return tArray;
        }

        /// <summary>Sets the capacity to the actual number of elements in the <see cref="QueueBuffer{T}" />, if that number is less than 90 percent of current capacity.</summary>
        public void TrimExcess()
        {
            int length = (int)((double)((int)this._array.Length) * 0.9);
            if (this._size < length)
            {
                this.SetCapacity(this._size);
            }
        }

        /// <summary>Enumerates the elements of a <see cref="T:System.Collections.Generic.Queue`1" />.</summary>
        [Serializable]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private QueueBuffer<T> _q;

            private int _index;

            private int _version;

            private T _currentElement;


            public T Current
            {
                get
                {
                    if (this._index < 0)
                    {
                        if (this._index != -1)
                        {
                            throw new InvalidOperationException("Enumerator ended");
                        }
                        else
                        {
                            throw new InvalidOperationException("Enumerator not started");
                        }
                    }
                    return this._currentElement;
                }
            }


            object System.Collections.IEnumerator.Current
            {

                get
                {
                    if (this._index < 0)
                    {
                        if (this._index != -1)
                        {
                            throw new InvalidOperationException("Enumerator ended");
                        }
                        else
                        {
                            throw new InvalidOperationException("Enumerator not started");
                        }
                    }
                    return this._currentElement;
                }
            }

            internal Enumerator(QueueBuffer<T> q)
            {
                this._q = q;
                this._version = this._q._version;
                this._index = -1;
                this._currentElement = default(T);
            }

            public void Dispose()
            {
                this._index = -2;
                this._currentElement = default(T);
            }

            public bool MoveNext()
            {
                if (this._version != this._q._version)
                {
                    throw new InvalidOperationException("Enumerator is not in newest version");
                }
                if (this._index == -2)
                {
                    return false;
                }
                this._index = this._index + 1;
                if (this._index == this._q._size)
                {
                    this._index = -2;
                    this._currentElement = default(T);
                    return false;
                }
                this._currentElement = this._q.GetElement(this._index);
                return true;
            }


            void System.Collections.IEnumerator.Reset()
            {
                if (this._version != this._q._version)
                {
                    throw new InvalidOperationException("Enumerator is not in newest version");
                }
                this._index = -1;
                this._currentElement = default(T);
            }
        }
    }
}