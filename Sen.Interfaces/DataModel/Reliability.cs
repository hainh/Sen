using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public enum Reliability : byte
    {
        /// <summary>
        /// <c>Reliable</c>
        /// All messages are guaranteed to be delivered, the order is not guaranteed, duplicates are dropped.
        /// Uses a fixed sliding window.
        /// </summary>
        ReliableUnorderd,

        /// <summary>
        /// <c>ReliableSequenced</c>
        /// All messages are guaranteed to be delivered with the order also being guaranteed, duplicates are dropped.
        /// Uses a fixed sliding window.
        /// </summary>
        ReliableOrderedSequenced,

        /// <summary>
        /// <c>ReliableSequencedFragmented</c>
        /// All messages are guaranteed to be delivered with the order also being guaranteed, duplicates are dropped.
        /// Uses a fixed sliding window. Allows large messages to be fragmented.
        /// </summary>
        ReliableOrderedSequencedFragmented,

        /// <summary>
        /// <c>Unreliable</c>
        /// Delivery is not guaranteed nor is the order. Duplicates are dropped.
        /// </summary>
        Unreliable,

        /// <summary>
        /// <c>UnreliableOrdered</c>
        /// Delivery is not guaranteed but the order is. Older packets and duplicates are dropped.
        /// </summary>
        UnreliableOrdered,

        /// <summary>
        /// <c>ReliableOrdered</c>
        /// All messages are not guaranteed to be delivered.
        /// If you send multiple messages, at least one is guranteed to arrive.
        /// If you send a single message, it is guaranteed to arrive.
        /// Messages will always be in order. Duplicates are dropped.
        /// </summary>
        ReliableOrdered,

        /// <summary>
        /// <c>ReliableFragmented</c>
        /// All messages are guaranteed to be delivered, the order is not guaranteed, duplicates are dropped.
        /// Uses a fixed sliding window. Allows large messages to be fragmented.
        /// </summary>
        ReliableFragmented,
    }
}
