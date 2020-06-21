using MessagePack;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Sen
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
    public abstract class AbstractPlayer<TUnionData, TGrainState> : BaseScheduleGrain<TGrainState>, IPlayer
         where TUnionData : IUnionData
    {
        public const string ProxyStream = "ProxyStream";
        public const string SMSProvider = "SMSProvider";

        protected IRoom _room;
        protected bool _isBot;

        public IPEndPoint LocalAddress { get; private set; }
        public IPEndPoint RemoteAddress { get; private set; }
        public ValueTask<IRoom> GetRoom() => new ValueTask<IRoom>(_room);

        public abstract string Name { get; }

        public ValueTask<string> GetName() => new ValueTask<string>(Name);

        protected IAsyncStream<Immutable<byte[]>> _stream;

        public ValueTask SetRoomJoined(IRoom room)
        {
            _room = room;
            return default;
        }

        public ValueTask<bool> IsBot() => new ValueTask<bool>(_isBot);

        /// <summary>
        /// Get a game world instance.
        /// </summary>
        /// <returns>IGameWorld instance that this game system lay on</returns>
        protected abstract ILobby GetGameWorld();

        /// <summary>
        /// Check if this <paramref name="accessToken"/> is valid. This means user authenticated successfully.
        /// </summary>
        /// <param name="accessToken">Access token to check with</param>
        /// <returns><code>true</code> if authenticated successfully, <code>false</code> otherwise</returns>
        protected abstract ValueTask<bool> CheckAccessToken(string accessToken);

        public virtual async ValueTask<bool> InitConnection(EndPoint local, EndPoint remote, string accessToken)
        {
            if (LocalAddress != null)
            {
                return true;
            }

            LocalAddress = local as IPEndPoint;
            RemoteAddress = remote as IPEndPoint;

            if (!await CheckAccessToken(accessToken))
            {
                return false;
            }

            ILobby gameWorld = GetGameWorld();
            await gameWorld.JoinRoom(this, Name);
            return true;
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
            return default;
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

        public async ValueTask Write(Immutable<IUnionData> message, WiredDataType underlieData = WiredDataType.Normal)
        {
            WiredData<TUnionData> wiredData = new WiredData<TUnionData>
            {
                Data = (TUnionData)message.Value
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
                Data = (TUnionData)message
            };
            byte[] rawData = MessagePackSerializer.Serialize(wiredData);
            await Task.WhenAll(players.Select(player => (player as AbstractPlayer<TUnionData, TGrainState>).Write(rawData)));
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
