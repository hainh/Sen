using MessagePack;
using Microsoft.Extensions.ObjectPool1;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public abstract class BaseGrain : Grain
    {
        public static readonly TimeSpan INFINITE_TIMESPAN = TimeSpan.FromMilliseconds(-1);
        /// <summary>
        /// Schedule a callback to call once after <paramref name="dueTime"/>.
        /// <para>
        /// See also <seealso cref="Grain.RegisterTimer(Func{object, Task}, object, TimeSpan, TimeSpan)"/>
        /// </para>
        /// </summary>
        /// <param name="asyncCallback">Callback to call</param>
        /// <param name="state">State to pass to callback</param>
        /// <param name="dueTime">Time to wait before callback execution</param>
        /// <returns>A disposable handler to cancel the schedule</returns>
        protected IDisposable Schedule(Func<object, Task> asyncCallback, object state, TimeSpan dueTime)
        {
            return RegisterTimer(asyncCallback, state, dueTime, INFINITE_TIMESPAN);
        }

        /// <summary>
        /// Schedule a callback to call in interval.
        /// <para>
        /// See also <seealso cref="Grain.RegisterTimer(Func{object, Task}, object, TimeSpan, TimeSpan)"/>
        /// </para>
        /// </summary>
        /// <param name="asyncCallback">Callback to call</param>
        /// <param name="state">State to pass to callback</param>
        /// <param name="dueTime">Time to wait before first callback execution</param>
        /// <param name="period">Time to repeat subsequence callback</param>
        /// <returns>A disposable handler to cancel the schedule</returns>
        protected IDisposable ScheduleInterval(Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            return RegisterTimer(asyncCallback, state, dueTime, period);
        }
    }

    public abstract class NetworkRpcGrain : Grain
    {
        /// <summary>
        /// Use <c>HandleMessage</c> mechanism to handle message in RPC-like
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        protected async ValueTask<Immutable<byte[]>> HandleData<T>(Immutable<byte[]> data) where T : IUnionData
        {
            NetworkOptions networkOptions = NetworkOptions.Create((ushort)((data.Value[1] << 8) | data.Value[0]));
            var rawData = new ReadOnlyMemory<byte>(data.Value, sizeof(ushort), data.Value.Length - sizeof(ushort));
            T wiredData = MessagePackSerializer.Deserialize<T>(rawData);
            T returnedData = (T)await((dynamic)this).HandleMessage((dynamic)wiredData, networkOptions);
            if (returnedData != null)
            {
                byte[] returnedBytes = SerializeData(returnedData, networkOptions);
                NetworkOptions.Return(networkOptions);
                return new Immutable<byte[]>(returnedBytes);
            }
            else
            {
                NetworkOptions.Return(networkOptions);
                return default;
            }
        }

        public static byte[] SerializeData<T>(T message, NetworkOptions networkOptions)
        {
            var memStream = _memStreamPool.Get();
            ushort serviceCode = networkOptions.ToServiceCode();
            memStream.WriteByte((byte)serviceCode);
            memStream.WriteByte((byte)(serviceCode >> 8));
            MessagePack.MessagePackSerializer.Serialize(memStream, message);

            byte[] data = memStream.ToArray();
            _memStreamPool.Return(memStream);
            return data;
        }

        private static readonly ObjectPool<MemoryStream> _memStreamPool
            = new DefaultObjectPool<MemoryStream>(new MemoryStreamPooledObjectPolicy(), MemoryStreamPooledObjectPolicy.MaximumRetained);
    }

    public class MemoryStreamPooledObjectPolicy : PooledObjectPolicy<MemoryStream>
    {
        public override MemoryStream Create()
        {
            return new MemoryStream(1024);
        }

        public const int MaximumRetained = 512;
        private const int MaximunLargeStreamRetain = MaximumRetained / 4;

        private const int MaxStreamCapacity = 128 * 1024;
        private const int LargeStreamCapacity = 8 * 1024;

        private int _countLargeStream = 0;
        public override bool Return(MemoryStream obj)
        {
            if (obj.Capacity > MaxStreamCapacity)
            {
                return false;
            }
            if (obj.Capacity > LargeStreamCapacity)
            {
                if (_countLargeStream < MaximunLargeStreamRetain)
                {
                    ++_countLargeStream;
                }
                else
                {
                    return false;
                }
            }
            obj.SetLength(0);
            return true;
        }

        public override void OnCreateFromPool(MemoryStream obj)
        {
            if (obj.Capacity > LargeStreamCapacity && _countLargeStream > 0)
            {
                --_countLargeStream;
            }
        }
    }
}
