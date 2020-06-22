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

        public ValueTask<long> GetMatchId() => new ValueTask<long>(State.MatchId);

        public ValueTask<string> GetRoomName() => new ValueTask<string>(State.RoomName);

        public ValueTask<string> GetPassword() => new ValueTask<string>(State.Password);

        public ValueTask<int> GetPlayerLimit() => new ValueTask<int>(State.PlayerLimit);

        public ValueTask<ILobby> GetParent() => new ValueTask<ILobby>(State.Parent);

        public virtual ValueTask<bool> IsLobby() => new ValueTask<bool>(false);

        public ValueTask<bool> IsFull() => new ValueTask<bool>(State.Players.Count >= State.PlayerLimit);

        public ValueTask<ICollection<IPlayer>> GetPlayers() => new ValueTask<ICollection<IPlayer>>(State.Players.Values);

        /// <summary>
        /// Handle a message object. Inherited class create its own overloaded version to
        /// handle a specific message.
        /// </summary>
        /// <returns>A <see cref="IUnionData"/> will be serialized and returned to game client or null to send nothing</returns>
#pragma warning disable IDE0060 // Remove unused parameter
        protected ValueTask<IUnionData> HandleMessage(IUnionData message, IPlayer player) => default;
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Body of this method must be
        /// <code>return HandleMessage((dynamic)message, player);</code>
        /// to call appropriate <see cref="HandleMessage"/> overload for each message type.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public ValueTask<IUnionData> HandleRoomMessage(IUnionData message, IPlayer player)
        {
            return ((dynamic)this).HandleMessage((dynamic)message, player);
        }

        public ValueTask<bool> JoinRoom(IPlayer player, string playerName)
        {
            if (!IsFull().Result)
            {
                if (!State.Players.ContainsKey(playerName))
                {
                    OnPlayerJoining(player);
                    State.Players.Add(playerName, player);
                    OnPlayerJoined(player);
                    return new ValueTask<bool>(true);
                }
            }

            return new ValueTask<bool>(false);
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
