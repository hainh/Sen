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
    public abstract class AbstractPlayer<TUnionData, TState> : BaseGrain, IPlayer where TState : IPlayerState
         where TUnionData : IUnionData
    {
        private IClientObserver? _clientObserver;

        public int LocalPort { get; private set; }
        public string? RemoteAddress { get; private set; }
        public ValueTask<IRoom?> GetRoom() => new(profile.State.Room);

        public string Name => profile.State.Name;

        public ValueTask<string> GetName() => new(Name);

        public async ValueTask<bool> JoinRoom(IRoom room)
        {
            if (profile.State.Room != null)
            {
                await LeaveRoom();
            }
            if (await room.JoinRoom(Me, Name))
            {
                profile.State.Room = room;
                await profile.WriteStateAsync();
                return true;
            }
            return false;
        }

        public async ValueTask<bool> LeaveRoom()
        {
            if (profile.State.Room == null)
            {
                return false;
            }
            var result = await profile.State.Room.LeaveRoom(Me, Name);
            profile.State.Room = null;
            await profile.WriteStateAsync();
            return result;
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

        private IPlayer? _me = null;
        protected IPlayer Me => _me ??= this.AsReference<IPlayer>();
        protected readonly IPersistentState<TState> profile;

        public AbstractPlayer(IPersistentState<TState> profile)
        {
            this.profile = profile;
        }

        /// <inheritdoc />
        public virtual async ValueTask InitConnection(int localPort, string remote, IClientObserver observer)
        {
            LocalPort = localPort;
            RemoteAddress = remote;

            _clientObserver = observer;
            ILobby gameWorld = GetGameWorld();
            await gameWorld.JoinRoom(Me, Name);
            await JoinRoom(gameWorld);
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
            return default!;
        }

        /// <summary>
        /// Read data from client. Calling <see cref="HandleMessage"/> overloaded mothods in this class.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ValueTask<Immutable<byte[]>> OnReceivedData(Immutable<byte[]> data)
        {
            return HandleData<TUnionData>(data);
        }

        public ValueTask SendData(Immutable<IUnionData> message, NetworkOptions networkOptions)
        {
            byte[] rawData = SerializeData((TUnionData)message.Value, networkOptions);
            return SendData(rawData.AsImmutable());
        }

        public ValueTask SendData(Immutable<byte[]> raw)
        {
            if (_clientObserver != null && raw.Value != null)
            {
                _clientObserver.ReceiveData(raw);
            }
            return ValueTask.CompletedTask;
        }

        public static void Broadcast(TUnionData message, IEnumerable<IPlayer> players, NetworkOptions networkOptions)
        {
            var rawData = SerializeData(message, networkOptions).AsImmutable();
            foreach (var player in players)
            {
                player.SendData(rawData);
            }
        }

        //protected abstract void SerializeData(MemoryStream stream, I)

        public ValueTask Disconnect()
        {
            Console.WriteLine("Disconnect " + RemoteAddress?.ToString());
            return ValueTask.CompletedTask;
        }

        public abstract ValueTask OnDisconnect();

        public override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        public override Task OnDeactivateAsync()
        {
            return profile.WriteStateAsync();
        }
    }
}
