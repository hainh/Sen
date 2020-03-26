using Akka.Actor;
using Senla.Core;
using Senla.Core.Utilities;
using Senla.Gamer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senla.Gamer
{
    /// <summary>
    /// Room base class.
    /// A specific game is implemented in a subclass of Room.
    /// </summary>
    public abstract class Room : ReceiveActor
    {
        private bool disposed = false;

        private long _matchId;

        /// <summary>
        /// Match ID in room, not unique per room.
        /// Represented by the time match take place.
        /// </summary>
        public long MatchId { get { return _matchId; } }

        /// <summary>
        /// Name of the room. Used for searching room.
        /// </summary>
        public string RoomName { get; private set; }

        /// <summary>
        /// Maximum number of players can join room.
        /// </summary>
        public short PlayerLimit { get; set; }

        /// <summary>
        /// Room with password protected.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Indicate if room is password protected.
        /// </summary>
        public bool IsPrivate { get { return Password != null && string.Empty != Password; } }

        public LobbyBase Parent { get; private set; }

        protected bool LogTransmitDataEnabled { get; set; }

        protected IEntityService<ITransmitData> TransmitDataLogService { get; set; }

        protected Func<ITransmitData> LogTransmitDataFactory;

        /// <summary>
        /// Fast way to detect a lobby.
        /// </summary>
        public bool IsLobby { get; internal set; }

        public bool IsFull
        {
            get
            {
                return PlayersCount >= PlayerLimit;
            }
        }

        public bool JoinLeaveBroadCastEnable { get; set; }

        public List<Player> Players { get; protected set; }

        public int PlayersCount 
        { 
            get
            {
                return Players.Count(p => p != null);
            }
        }

        public delegate void OperationHandler(byte opCode, OperationData operationRequest, Player player, SendParameters sendParameters);

        protected Dictionary<byte, OperationHandler> OperationHandlers { get; private set; }
        protected Dictionary<byte, OperationHandler> ExtendedOperationHandlers { get; private set; }

        public delegate void JoinLeaveHandler(Player player);

        public event JoinLeaveHandler PlayerJoined;

        public event JoinLeaveHandler PlayerLeft;

        protected virtual void OnPlayerJoined(Player joinedPlayer)
        {
            if (PlayerJoined != null)
            {
                PlayerJoined(joinedPlayer);
            }
        }

        protected virtual void OnPlayerLeft(Player leftPlayer)
        {
            if (PlayerLeft != null)
            {
                PlayerLeft(leftPlayer);
            }
        }

        public Room(short playerLimit)
        {
            PlayerLimit = playerLimit;
            RoomName = GetRoomName();
            Players = new List<Player>(Math.Min(1000, (int)playerLimit));
            OperationHandlers = new Dictionary<byte, OperationHandler>(5);
            ExtendedOperationHandlers = new Dictionary<byte, OperationHandler>(15);
            JoinLeaveBroadCastEnable = true;
            IsLobby = false;
            LogTransmitDataEnabled = false;

            Initialize();
        }

        public Room()
            :this(4)
        {
        }

        public Room(short playerLimit, LobbyBase parentLobby)
            : this(playerLimit)
        {
            this.Parent = parentLobby;
        }

        public Room(LobbyBase parentLobby)
            : this()
        {
            this.Parent = parentLobby;
        }

        public virtual void OnOperationRequest(Player player, OperationData operationRequest, SendParameters sendParameters)
        {
            //LogTransmitData(operationRequest, player.Name);

            var opCode = (OperationCode)operationRequest.Code;
            switch(opCode)
            {
                case OperationCode.EXTENDED_OPERATION:
                    {
                        var customOpCode = (byte)operationRequest[DataCode.EXTENDED_CODE];
                        
                        OperationHandler handler;
                        ExtendedOperationHandlers.TryGetValue(customOpCode, out handler);
                        if (handler != null)
                        {
                            handler(customOpCode, operationRequest, player, sendParameters);
                        }
                    }
                    break;
                case OperationCode.CHAT_ROOM:
                    {
                        string msg = (string)operationRequest[DataCode.CHAT_MESSAGE];

                        Player.BroadCastEvent(
                            new EventData(EventCode.CHAT_ROOM, new Dictionary<byte, object>()
                                {
                                    { DataCode.CHAT_MESSAGE, msg }
                                }
                            ), Players, sendParameters);
                    }
                    break;
                default:
                    {
                        OperationHandler handler;
                        OperationHandlers.TryGetValue(operationRequest.Code, out handler);
                        if(handler != null)
                        {
                            handler(operationRequest.Code, operationRequest, player, sendParameters);
                        }
                    }
                    break;
            }
        }

        public virtual void AddExtendedOperationHandler(byte opCode, OperationHandler functionCallback)
        {
            ExtendedOperationHandlers.Add(opCode, functionCallback);
        }

        public virtual void AddOperationHandler(byte opCode, OperationHandler functionCallBack)
        {
            OperationHandlers.Add(opCode, functionCallBack);
        }

        /// <summary>
        /// All room must register operation handlers.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Set MatchId for the game.
        /// </summary>
        public virtual void GenerateNextMatchId()
        {
            _matchId = (long)(DateTime.UtcNow - new DateTime(2015, 1, 1)).TotalMilliseconds;
        }

        /// <summary>
        /// Replace players in case 2 device log in same account. This is another case of joining a game.
        /// </summary>
        /// <param name="oldPlayer"></param>
        /// <param name="newPlayer"></param>
        public virtual void ReplacePlayer(Player oldPlayer, Player newPlayer, SendParameters sendParameters)
        {
#if DEBUG
            if (oldPlayer.Name != newPlayer.Name)
            {
                throw new ArgumentException("Player's name not match.");
            }
#endif
            // Only replacing Peers is its nature, keep oldPlayer object 
            // because all Player refference in a room is connected to the oldPlayer
            oldPlayer.Peer = newPlayer.Peer;
            newPlayer.Peer.Tell(new PeerActor.ReplacePlayer(oldPlayer));

            newPlayer = oldPlayer;

            //Application.AppInstance.GamesManager.PlayerChangedRoom(oldPlayer);

            var currentStateData = GetDataOnJoinRoom(newPlayer);
            if (!currentStateData.ContainsKey(DataCode.RESULT))
            {
                currentStateData.Add(DataCode.RESULT, (byte)0);
            }
            if (!currentStateData.ContainsKey(DataCode.ROOM_NAME))
            {
                currentStateData.Add(DataCode.ROOM_NAME, RoomName);
            }
            var resp = new OperationData(OperationCode.JOIN_ROOM, currentStateData);
            newPlayer.SendOperationResponse(resp, sendParameters);
        }

        public virtual void JoinRoom(Player player, SendParameters sendParameters)
        {
            if (!IsFull)
            {
                var newPlayer = CreateNewPlayerJoinRoom(player);
                player.LeaveCurrentRoom(false);
                AddPlayer(newPlayer);

                var currentStateData = GetDataOnJoinRoom(newPlayer);
                if (!currentStateData.ContainsKey(DataCode.RESULT))
                {
                    currentStateData.Add(DataCode.RESULT, (byte)0);
                }
                if (!currentStateData.ContainsKey(DataCode.ROOM_NAME))
                {
                    currentStateData.Add(DataCode.ROOM_NAME, RoomName);
                }
                var resp = new OperationData(OperationCode.JOIN_ROOM, currentStateData);
                newPlayer.SendOperationResponse(resp, sendParameters);


                if (JoinLeaveBroadCastEnable)
                {
                    // Send join_room event to all other players in this room
                    var listPlayers = Players.Where(p => p != null && p != newPlayer);

                    EventData eventData = new EventData(EventCode.BROADCAST_JOIN_ROOM, GetJoinBroadCastData(newPlayer));
                    Player.BroadCastEvent(eventData, listPlayers, sendParameters);
                }

                OnPlayerJoined(newPlayer);
            }
            else
            {
                var resp = new OperationData(OperationCode.JOIN_ROOM, new Dictionary<byte, object> { { DataCode.RESULT, (byte)3 } });
                player.SendOperationResponse(resp, sendParameters);
            }
        }

        /// <summary>
        /// Add new Player to the Room
        /// </summary>
        /// <param name="player"></param>
        protected virtual void AddPlayer(Player player)
        {
            for (int i = 0; i < PlayerLimit; ++i)
            {
                if (i >= Players.Count)
                {
                    Players.Add(player);
                    break;
                }
                else if (Players[i] == null)
                {
                    Players[i] = player;
                    break;
                }
            }

            player.Room = Self;

            ApplicationBase.Instance.GamesManager.PlayerChangedRoom(player);
        }

        /// <summary>
        /// This method is called when the player leave room or disconnected. If <see cref="Application.BotEnabled"/>
        /// and <see cref="CanCompletelyRemovePlayer(Player, bool)"/> == false then this <see cref="Player"/> will be a BOT.
        /// All subclass must implement its own Bot.
        /// Remove player preserve all other players position in the player list.
        /// </summary>
        /// <param name="player"></param>
        protected internal virtual void RemovePlayer(Player player, bool onDisconnected)
        {
            if (Application.AppInstance.BotEnabled && !CanCompletelyRemovePlayer(player, onDisconnected))
            {
                // Make player a bot
                player.ResetPeer(onDisconnected);
                player.WaitingReconnect = onDisconnected;
                player.LastMatchId = MatchId;

                OnBecomeBot(player);

                if (onDisconnected)
                {
                    Application.AppInstance.GamesManager.PlayerDisconnected(player, true);
                }
            }
            else
            {
                if (JoinLeaveBroadCastEnable)
                {
                    var listPlayers = Players.Where(p => p != null && p != player);
                    EventData eventData = new EventData(EventCode.BROADCAST_LEAVE_ROOM, GetLeaveBroadCastData(player));
                    Player.BroadCastEvent(eventData, listPlayers, new SendParameters());
                }

                var playerIndex = Players.IndexOf(player);
                if (IsLobby)
                {
                    Players.RemoveAt(playerIndex);
                }
                else
                {
                    Players[playerIndex] = null;
                }

                if (onDisconnected)
                {
                    player.ResetPeer(onDisconnected);
                    ApplicationBase.Instance.PlayerDisconnected(player, false);
                }

                OnPlayerLeft(player);
            }
        }

        /// <summary>
        /// Determine if this player can completely be removed form room.
        /// Called in <see cref="RemovePlayer(Player, bool)"/>
        /// </summary>
        /// <param name="player">Player to remove</param>
        /// <returns>true if the player can be completely removed, 
        /// false if player wants to become a Bot
        /// </returns>
        protected virtual bool CanCompletelyRemovePlayer(Player player, bool disconnected)
        {
            return true;
        }

        public virtual bool AllowPlayerLeave(Player player)
        {
            return true;
        }

        /// <summary>
        /// On player becomes a bot callback.
        /// This method is called when player is removed from room and become a bot in <seealso cref="RemovePlayer(Player, bool)"/>
        /// </summary>
        /// <param name="player"></param>
        protected virtual void OnBecomeBot(Player player)
        {
        }

        /// <summary>
        /// Create specific player for this game Room. This player is used for this game only.
        /// </summary>
        /// <param name="player">player form last game</param>
        /// <returns></returns>
        protected abstract Player CreateNewPlayerJoinRoom(Player player);

        /// <summary>
        /// Kick player that cheat.
        /// </summary>
        /// <param name="player"></param>
        protected void KickPlayerSentWrongData(Player player, string reason = "")
        {
#if DEBUG
            player.SendOperationResponse(new OperationData(OperationCode.PLAYER_REMOVED,
                                                new Dictionary<byte, object> { { DataCode.REASON, reason} }),
                                         new SendParameters());
#endif
            // This player cannot handle any "in room operation request" or be sent any event and response
            //player.Room = null;
            //player.Peer = null;
        }

        /// <summary>
        /// If Room.JoinLeaveBroadCastEnable = true, this data will be sent to all other players
        /// </summary>
        /// <returns>All parameters to send to clients on broadcasting</returns>
        protected virtual Dictionary<byte, object> GetLeaveBroadCastData(Player leavingPlayer)
        {
            return new Dictionary<byte, object>();
        }

        /// <summary>
        /// If Room.JoinLeaveBroadCastEnable = true, this data will be sent to all other players
        /// </summary>
        /// <returns>All parameters to send to clients on broadcasting</returns>
        protected virtual Dictionary<byte, object> GetJoinBroadCastData(Player joiningPlayer)
        {
            return new Dictionary<byte, object>();
        }

        /// <summary>
        /// Game data to send to the newly join player. Must implement this method intensively to make game work flawlessly.
        /// Known issue: Crash if we dont send room data immediately after player join room, other activities in game may occur and send 
        /// data to client when things in client is not set up.
        /// </summary>
        /// <param name="newPlayer">new player join room</param>
        /// <returns>Data to send to client</returns>
        protected abstract Dictionary<byte, object> GetDataOnJoinRoom(Player newPlayer);

        /// <summary>
        ///  Will be called to create room name
        /// </summary>
        /// <returns>A distinct RoomName to distinguish with other Room</returns>
        protected abstract string GetRoomName();

        protected void Schedule(Action action, int firstInMs)
        {
            Context.System.Scheduler.ScheduleTellOnceCancelable(firstInMs, Self, action, Self);
        }

        protected void ScheduleOnInterval(Action action, int firstInMs, int regularInMs)
        {
            Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(firstInMs, regularInMs, Self, action, Self);
        }

        //public virtual void LogTransmitData(object dataTransmitted, string player)
        //{
        //    if (!LogTransmitDataEnabled)
        //    {
        //        return;
        //    }

        //    var data = LogTransmitDataFactory();
        //    if (dataTransmitted is OperationResponse)
        //    {
        //        var d = dataTransmitted as OperationResponse;
        //        data.Code = d.OperationCode;
        //        data.Data = d.Parameters;
        //        data.Type = "OpResponse";
        //    }
        //    else if (dataTransmitted is OperationRequest)
        //    {
        //        var d = dataTransmitted as OperationRequest;
        //        data.Code = d.OperationCode;
        //        data.Data = d.Parameters;
        //        data.Type = "OpRequest";
        //    }
        //    else if (dataTransmitted is EventData)
        //    {
        //        var d = dataTransmitted as EventData;
        //        data.Code = d.Code;
        //        data.Data = d.Parameters;
        //        data.Type = "Event";
        //    }
        //    else
        //    {
        //        throw new ArgumentException("Not support object type in argument", "dataTransmitted");
        //    }

        //    data.Room = RoomName;
        //    data.Players = player;
        //    data.MatchId = MatchId;
        //    data.ConvertToReadableParameters();

        //    TransmitDataLogService.Create(data);
        //}
    }
}
