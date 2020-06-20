using MessagePack;
using Orleans;
using Sen.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Game
{
    public delegate void PlayerChange(IPlayer player);

    /// <summary>
    /// Base class for game rooms.
    /// <para>
    /// Subclass will overload <see cref="HandleMessage(IUnionData, IPlayer)"/> method to
    /// handle each message type from player
    /// </para>
    /// </summary>
    public abstract class Room<TGrainState> : Grain<TGrainState>, IRoom
    {
        protected long _matchId;
        protected string _roomName;
        protected string _password;
        protected int _playerLimit;
        protected ILobby _parent;
        protected IDictionary<string, IPlayer> _players;

        public event PlayerChange PlayerJoined;

        public event PlayerChange PlayerLeft;

        public event PlayerChange PlayerJoining;

        public event PlayerChange PlayerLeaving;

        public ValueTask<long> GetMatchId() => new ValueTask<long>(_matchId);

        public ValueTask<string> GetRoomName() => new ValueTask<string>(_roomName);

        public ValueTask<string> GetPassword() => new ValueTask<string>(_password);

        public ValueTask<int> GetPlayerLimit() => new ValueTask<int>(_playerLimit);

        public ValueTask<ILobby> GetParent() => new ValueTask<ILobby>(_parent);

        public ValueTask<bool> IsLobby() => new ValueTask<bool>(false);

        public ValueTask<bool> IsFull() => new ValueTask<bool>(_players.Count >= _playerLimit);

        public ValueTask<ICollection<IPlayer>> GetPlayers() => new ValueTask<ICollection<IPlayer>>(_players.Values);

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

        public async ValueTask<bool> JoinRoom(IPlayer player)
        {
            if (!IsFull().Result)
            {
                string playerName = await player.GetName();
                if (!_players.ContainsKey(await player.GetName()))
                {
                    OnPlayerJoining(player);
                    _players.Add(playerName, player);
                    OnPlayerJoined(player);
                    return true;
                }
            }

            return false;
        }

        protected virtual async ValueTask<bool> RemovePlayer(IPlayer player)
        {
            return _players.Remove(await player.GetName());
        }

        public ValueTask SetParent(ILobby room)
        {
            _parent = room;
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
