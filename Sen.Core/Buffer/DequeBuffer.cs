using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Buffer
{
    public class DequeBuffer<T> : QueueBuffer<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DequeBuffer{T}" /> class that
        /// is empty and has the default initial capacity.
        /// </summary>
        public DequeBuffer()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DequeBuffer{T}" /> class that 
        /// is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="DequeBuffer{T}" /> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="capacity" /> is less than zero.</exception>
        public DequeBuffer(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DequeBuffer{T}" /> class that contains 
        /// elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see cref="DequeBuffer{T}" />.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="collection" /> is null.</exception>
        public DequeBuffer(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Adds an object to the head of the <see cref="DequeBuffer{T}" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="DequeBuffer{T}" />. 
        /// The value can be null for reference types.</param>
        public void PushHead(T item)
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
            int length1 = this._array.Length;
            this._head = (this._head - 1 + length1) % length1;
            this._array[this._head] = item;
            this._size = this._size + 1;
            this._version = this._version + 1;
        }

        public void PushHead(IEnumerable<T> items)
        {
            foreach (var item in items.Reverse())
            {
                PushHead(item);
            }
        }
    }
}
