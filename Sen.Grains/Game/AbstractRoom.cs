using Orleans;
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
    public abstract class AbstractRoom<TGrainState> : BaseScheduleGrain<TGrainState>, IRoom where TGrainState : IRoomState
    {
        public event PlayerChange PlayerJoined;

        public event PlayerChange PlayerLeft;

        public event PlayerChange PlayerJoining;

        public event PlayerChange PlayerLeaving;

        public ValueTask<long> GetMatchId() => new(State.MatchId);

        public ValueTask<string> GetRoomName() => new(State.RoomName);

        public ValueTask<string> GetPassword() => new(State.Password);

        public ValueTask<int> GetPlayerLimit() => new(State.PlayerLimit);

        public ValueTask<ILobby> GetParent() => new(State.Parent);

        public virtual ValueTask<bool> IsLobby() => new(false);

        public ValueTask<bool> IsFull() => new(State.Players.Count >= State.PlayerLimit);

        public ValueTask<ICollection<IPlayer>> GetPlayers() => new(State.Players.Values);

        private IRoom _me = null;
        protected IRoom Me => _me ??= this.AsReference<IRoom>();

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
            if (await IsFull())
            {
                if (!State.Players.ContainsKey(playerName))
                {
                    OnPlayerJoining(player);
                    State.Players.Add(playerName, player);
                    OnPlayerJoined(player);
                    return true;
                }
            }

            return false;
        }

        protected virtual async ValueTask<bool> RemovePlayer(IPlayer player)
        {
            return State.Players.Remove(await player.GetName());
        }

        public ValueTask SetParent(ILobby room)
        {
            State.Parent = room;
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
