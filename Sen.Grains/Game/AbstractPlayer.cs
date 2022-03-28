using MessagePack;
using Microsoft.Extensions.ObjectPool1;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Sen
{
    /// <summary>
    /// Base class for proxy connection to silo.
    /// <para>
    /// Subclass will overload <see cref="HandleMessage(TUnionData, NetworkOptions)"/> method to handle each message
    /// type from client.
    /// </para>
    /// </summary>
    /// <typeparam name="TUnionData">MessagePack's Union interface root of all message types</typeparam>
    /// <typeparam name="TGrainState">Grain state</typeparam>
    public abstract class AbstractPlayer<TUnionData, TState> : BaseScheduleGrain, IPlayer where TState : IPlayerState
         where TUnionData : IUnionData
    {
        private IClientObserver _clientObserver;

        public string LocalAddress { get; private set; }
        public string RemoteAddress { get; private set; }
        public ValueTask<IRoom> GetRoom() => new(profile.State.Room);

        public string Name => profile.State.Name;

        public ValueTask<string> GetName() => new(Name);

        public ValueTask SetRoomJoined(IRoom room)
        {
            profile.State.Room = room;
            return default;
        }

        public ValueTask<bool> IsBot() => new(profile.State.IsBot);

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

        private IPlayer _me = null;
        protected IPlayer Me => _me ??= this.AsReference<IPlayer>();
        protected readonly IPersistentState<TState> profile;

        public AbstractPlayer(IPersistentState<TState> profile)
        {
            this.profile = profile;
        }

        /// <inheritdoc />
        public virtual async ValueTask<bool> InitConnection(string local, string remote, string username, string accessToken, IClientObserver observer)
        {
            if (LocalAddress != null)
            {
                return true;
            }

            if (profile.State.Name == null)
            {
                profile.State.Name = username;
            }
            else if (profile.State.Name != username)
            {
                return false;
            }
            LocalAddress = local;
            RemoteAddress = remote;

            if (!await CheckAccessToken(accessToken))
            {
                return false;
            }

            _clientObserver = observer;
            ILobby gameWorld = GetGameWorld();
            await gameWorld.JoinRoom(Me, Name);
            await SetRoomJoined(gameWorld);
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
        protected async ValueTask<TUnionData> HandleMessage(TUnionData message, NetworkOptions networkOptions)
        {
            if (profile.State.Room != null)
            {
                return (TUnionData)await profile.State.Room.HandleRoomMessage(message, Me, networkOptions);
            }
            return default;
        }

        /// <summary>
        /// Read data from client. Calling <see cref="HandleMessage"/> overloaded mothods in this class.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<Immutable<byte[]>> OnReceivedData(Immutable<byte[]> data)
        {
            NetworkOptions networkOptions = NetworkOptions.Create((ushort)((data.Value[1] << 8) | data.Value[0]));
            var rawData = new ReadOnlyMemory<byte>(data.Value, sizeof(ushort), data.Value.Length - sizeof(ushort));
            TUnionData wiredData = MessagePackSerializer.Deserialize<TUnionData>(rawData);
            dynamic message = wiredData;
            TUnionData returnedData = (TUnionData)await ((dynamic)this).HandleMessage(message, networkOptions);
            byte[] returnedBytes = null;
            if (returnedData != null)
            {
                returnedBytes = SerializeData(returnedData, networkOptions);
            }
            NetworkOptions.Return(networkOptions);
            return new Immutable<byte[]>(returnedBytes);
        }

        public ValueTask SendData(Immutable<IUnionData> message, NetworkOptions networkOptions)
        {
            byte[] rawData = SerializeData((TUnionData)message.Value, networkOptions);
            SendData(rawData.AsImmutable());
            return ValueTask.CompletedTask;
        }

        public ValueTask SendData(Immutable<byte[]> raw)
        {
            _clientObserver?.ReceiveData(raw);
            return ValueTask.CompletedTask;
        }

        public static ValueTask Broadcast(TUnionData message, IEnumerable<IPlayer> players, NetworkOptions networkOptions)
        {
            var rawData = SerializeData(message, networkOptions).AsImmutable();
            foreach (var player in players)
            {
                player.SendData(rawData);
            }
            return ValueTask.CompletedTask;
        }

        private static byte[] SerializeData(TUnionData message, NetworkOptions networkOptions)
        {
            var memStream = _memStreamPool.Get();
            ushort serviceCode = networkOptions.ToServiceCode();
            memStream.WriteByte((byte)serviceCode);
            memStream.WriteByte((byte)(serviceCode >> 8));
            MessagePackSerializer.Serialize(memStream, message);

            byte[] data = memStream.ToArray();
            _memStreamPool.Return(memStream);
            return data;
        }

        //protected abstract void SerializeData(MemoryStream stream, I)

        public ValueTask Disconnect()
        {
            Console.WriteLine("Disconnect " + RemoteAddress.ToString());
            return ValueTask.CompletedTask;
        }

        public abstract ValueTask OnDisconnect();

        public override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        private static readonly ObjectPool<MemoryStream> _memStreamPool = 
            new DefaultObjectPool<MemoryStream>(new MemoryStreamPooledObjectPolicy(), MemoryStreamPooledObjectPolicy.MaximumRetained);
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
