using MessagePack;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;
using Sen.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Game
{
    /// <summary>
    /// Base class for proxy connection to silo.
    /// <para>
    /// Subclass will overload <see cref="HandleMessage(TUnionData)"/> method to handle each message
    /// type from client.
    /// </para>
    /// </summary>
    /// <typeparam name="TUnionData">MessagePack's Union interface root of all message types</typeparam>
    /// <typeparam name="TGrainState">Grain state</typeparam>
    public abstract class Player<TUnionData, TGrainState> : Grain<TGrainState>, IPlayer
         where TUnionData : class, IUnionData
    {
        public static readonly TimeSpan INFINITE_TIMESPAN = TimeSpan.FromMilliseconds(-1);
        public const string ProxyStream = "ProxyStream";
        public const string SMSProvider = "SMSProvider";

        protected IRoom _room;
        protected bool _isBot;

        public IPEndPoint LocalAddress { get; private set; }
        public IPEndPoint RemoteAddress { get; private set; }
        public ValueTask<IRoom> GetRoom() => new ValueTask<IRoom>(_room);

        public abstract ValueTask<string> GetName();

        protected IAsyncStream<Immutable<byte[]>> _stream;

        public ValueTask SetRoomJoined(IRoom room)
        {
            _room = room;
            return default;
        }

        public ValueTask<bool> IsBot() => new ValueTask<bool>(_isBot);

        public virtual ValueTask<bool> InitConnection(EndPoint local, EndPoint remote, string accessToken)
        {
            if (LocalAddress != null)
            {
                return new ValueTask<bool>(true);
            }

            LocalAddress = local as IPEndPoint;
            RemoteAddress = remote as IPEndPoint;

            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// Handle a message object. Inherited class create its own overloaded version to
        /// handle a specific message.
        /// </summary>
        /// <remarks>
        /// This method is a fallback if the subclass has no public overload method for that specific
        /// <paramref name="message"/> parameter type.
        /// </remarks>
        /// <returns>A <see cref="IUnionData"/> will be serialized and returned to game client or null to send nothing</returns>
        protected async ValueTask<TUnionData> HandleMessage(TUnionData message)
        {
            if (_room != null)
            {
                return (TUnionData)await _room.HandleRoomMessage(message, this);
            }
            return null;
        }

        /// <summary>
        /// Read data from client. Calling <see cref="HandleMessage"/> overloaded mothods in this class.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<Immutable<byte[]>> Read(Immutable<byte[]> data)
        {
            WiredData<TUnionData> wiredData = MessagePackSerializer.Deserialize<WiredData<TUnionData>>(data.Value);
            dynamic message = wiredData.Data;
            TUnionData returnedData = await ((dynamic)this).HandleMessage(message);
            if (returnedData != null)
            {
                wiredData.Data = returnedData;
                return MessagePackSerializer.Serialize(wiredData).AsImmutable();
            }
            return new Immutable<byte[]>(null);
        }

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

        public async ValueTask Write(Immutable<IUnionData> message, WiredDataType underlieData = WiredDataType.Normal)
        {
            WiredData<TUnionData> wiredData = new WiredData<TUnionData>
            {
                Data = message.Value as TUnionData
            };
            byte[] rawData = MessagePackSerializer.Serialize(wiredData);
            await Write(rawData);
        }

        private async Task Write(byte[] raw)
        {
            try
            {
                await _stream.OnNextAsync(raw.AsImmutable());
            }
            catch (Exception)
            {
                await Task.Delay(100);
                // Retry once
                await _stream.OnNextAsync(raw.AsImmutable());
            }
        }

        public static async ValueTask Broadcast(IUnionData message, IEnumerable<IPlayer> players)
        {
            WiredData<TUnionData> wiredData = new WiredData<TUnionData>
            {
                Data = message as TUnionData
            };
            byte[] rawData = MessagePackSerializer.Serialize(wiredData);
            await Task.WhenAll(players.Select(player => (player as Player<TUnionData, TGrainState>).Write(rawData)));
        }

        public ValueTask Disconnect()
        {
            Console.WriteLine("Disconnect " + RemoteAddress.ToString());
            return default;
        }

        public abstract ValueTask OnDisconnect();

        public override Task OnActivateAsync()
        {
            IStreamProvider streamProvider = GetStreamProvider(SMSProvider);
            _stream = streamProvider.GetStream<Immutable<byte[]>>(this.GetPrimaryKey(), ProxyStream);
            return Task.CompletedTask;
        }
    }
}
