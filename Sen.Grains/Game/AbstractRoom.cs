using Orleans;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sen
{
    public delegate void PlayerChange(IPlayer player);

    /// <summary>
    /// Base class for game rooms.
    /// <para>
    /// Subclass will overload <see cref="HandleMessage(IUnionData, IPlayer)"/> method to
    /// handle each message type from player
    /// </para>
    /// </summary>
    public abstract class AbstractRoom<TState> : BaseScheduleGrain, IRoom where TState : IRoomState
    {
        public event PlayerChange PlayerJoined;

        public event PlayerChange PlayerLeft;

        public event PlayerChange PlayerJoining;

        public event PlayerChange PlayerLeaving;

        public ValueTask<long> GetMatchId() => new(persistent.State.MatchId);

        public ValueTask<string> GetRoomName() => new(persistent.State.RoomName);

        public ValueTask<string> GetPassword() => new(persistent.State.Password);

        public ValueTask<int> GetPlayerLimit() => new(persistent.State.PlayerLimit);

        public ValueTask<ILobby> GetParent() => new(persistent.State.Parent);

        public virtual ValueTask<bool> IsLobby() => new(false);

        public ValueTask<bool> IsFull() => new(persistent.State.Players.Count >= persistent.State.PlayerLimit);

        public ValueTask<ICollection<IPlayer>> GetPlayers() => new(persistent.State.Players.Values);

        private IRoom _me = null;
        protected IRoom Me => _me ??= this.AsReference<IRoom>();
        protected readonly IPersistentState<TState> persistent;

        public AbstractRoom(IPersistentState<TState> persistent)
        {
            this.persistent = persistent;
        }

        public override Task OnActivateAsync()
        {
            if (persistent.State.Players == null)
            {
                persistent.State.Players = new Dictionary<string, IPlayer>();
                persistent.State.PlayerLimit = 100;
            }
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            return persistent.WriteStateAsync();
        }

        /// <summary>
        /// Handle a message object. Inherited class create its own overloaded version to
        /// handle a specific message.
        /// </summary>
        /// <returns>A <see cref="IUnionData"/> will be serialized and returned to game client or null to send nothing</returns>
#pragma warning disable IDE0060 // Remove unused parameter
        protected ValueTask<IUnionData> HandleMessage(IUnionData message, IPlayer sender, NetworkOptions networkOptions) => default;
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Body of this method must be
        /// <code>return HandleMessage((dynamic)message, player);</code>
        /// to call appropriate <see cref="HandleMessage(IUnionData, IPlayer, NetworkOptions)"/> overload for each message type.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public ValueTask<IUnionData> HandleRoomMessage(IUnionData message, IPlayer sender, NetworkOptions networkOptions)
        {
            return ((dynamic)this).HandleMessage((dynamic)message, sender, networkOptions);
        }

        public async ValueTask<bool> JoinRoom(IPlayer player, string playerName)
        {
            if (!await IsFull())
            {
                if (!persistent.State.Players.ContainsKey(playerName))
                {
                    OnPlayerJoining(player);
                    persistent.State.Players.Add(playerName, player);
                    OnPlayerJoined(player);
                    await persistent.WriteStateAsync();
                    return true;
                }
            }

            return false;
        }

        public async ValueTask<bool> LeaveRoom(IPlayer player, string playerName)
        {
            if (persistent.State.Players.ContainsKey(playerName))
            {
                OnPlayerLeaving(player);
                persistent.State.Players.Remove(playerName);
                OnPlayerLeft(player);
                await persistent.WriteStateAsync();
                return true;
            }
            return false;
        }

        protected virtual async ValueTask<bool> RemovePlayer(IPlayer player)
        {
            return persistent.State.Players.Remove(await player.GetName());
        }

        public ValueTask SetParent(ILobby room)
        {
            persistent.State.Parent = room;
            return default;
        }

        protected virtual void OnPlayerJoined(IPlayer joinedPlayer)
        {
            PlayerJoined?.Invoke(joinedPlayer);
        }

        protected virtual void OnPlayerJoining(IPlayer joiningPlayer)
        {
            PlayerJoining?.Invoke(joiningPlayer);
        }

        protected virtual void OnPlayerLeft(IPlayer leftPlayer)
        {
            PlayerLeft?.Invoke(leftPlayer);
        }

        protected virtual void OnPlayerLeaving(IPlayer leavingPlayer)
        {
            PlayerLeaving?.Invoke(leavingPlayer);
        }

    }
}
