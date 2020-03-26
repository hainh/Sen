using Akka.Actor;
using Senla.Core;
using Senla.Gamer.Data;
using Senla.Gamer.Internal;
using Senla.Gamer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senla.Gamer
{
    /// <summary>
    /// Represent a player in a game (Room), each Room should have its own Player type.
    /// Extend this class to implement specific player for a Room and your own login method.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Room that Player belong to.
        /// </summary>
        public IActorRef Room { get; internal set; }

        /// <summary>
        /// Peer hold the connection with client
        /// </summary>
        public IActorRef Peer { get; internal set; }

        /// <summary>
        /// When player leave current Room intendly or disconnectedly, 
        /// this player instance will become a bot in game to continue to play.
        /// Bot must be enable by setting <seealso cref="Application.BotEnabled"/><c> = true</c> and <see cref="Room.CanCompletelyRemovePlayer(Player, bool)"/> return false.
        /// </summary>
        public bool IsBot { get { return Peer == null; } }

        /// <summary>
        /// This player is disconnected due to lost connection to client and then waiting for reconnection. This player is being a BOT.
        /// </summary>
        public bool WaitingReconnect { get; set; }

        /// <summary>
        /// Player's username
        /// </summary>
        public string Name
        {
            get { return UserData.UserName; }
        }

        /// <summary>
        /// Last match Id before disconnected
        /// </summary>
        public long LastMatchId { get; internal set; }

        /// <summary>
        /// Player's data
        /// </summary>
        public IUserData UserData { get; protected set; }

        public IUserDataService UserDataService { get; protected set; }

        /// <summary>
        /// Create a player
        /// </summary>
        /// <param name="peer">The connection with client</param>
        /// <param name="userDataService">Independant database service</param>
        public Player(IActorRef peer, IUserDataService userDataService)
        {
            this.Peer = peer;
            this.UserDataService = userDataService;

            this.UserData = peer.Ask<Player>(this).Result?.UserData ?? null;
            //if (peer.Player != null)
            //{
            //    this.UserData = peer.Player.UserData;
            //}
            //peer.Player = this;

        }

        /// <summary>
        /// Use for creating Bot only
        /// </summary>
        protected Player(IUserData userData)
        {
            UserData = userData;
            Peer = null;
            Room = null;
        }

        /// <summary>
        /// Try to login.
        /// </summary>
        /// <param name="username">username</param>
        /// <param name="password">password</param>
        /// <returns>true if logged in successfully, false if failed</returns>
        public virtual bool Login(string username, string password)
        {
            var data = UserDataService.Login(username, password);
            if (data == null)
                return false;
            this.UserData = data;
            data.LastTimeLogin = DateTime.UtcNow;
            return true;
        }

        public virtual IUserData SignUp(string username, string password)
        {
            if (UserDataService.IsUserExisted(username))
            {
                return null;
            }
            var userData = UserDataService.CreateNewUser(username, password);
            this.UserData = userData;
            return userData;
        }

        public virtual bool SaveUserData()
        {
            return UserDataService.Update(UserData);
        }

        /// <summary>
        /// Add more user information on signing up.
        /// </summary>
        /// <param name="userData">User's data object</param>
        /// <param name="operationRequest">The request data holder</param>
        public virtual void RegisterAdditionalInformation(OperationData extraInfo) { }

        /// <summary>
        /// Send a "Leave" message to player's room.
        /// </summary>
        /// <param name="onDisconnected">if player was disconnected from server.<seealso cref="Player.ResetPeer(bool)"/></param>
        public virtual void LeaveCurrentRoom(bool onDisconnected)
        {
            if (Room != null)
            {
                var oldRoom = Room;
                oldRoom.Tell(new Message.LeaveRoom(this, onDisconnected));
            }
        }

        /// <summary>
        /// Method to reset <code>Peer</code> if this Player is removed from a Room.
        /// This player will no longer recieve messages from its Room. <br/>
        /// Use cases:
        /// <list type="bullet">
        /// <item><term>Player leave its Room</term><description>
        /// Just assign Peer to null.
        /// </description></item>
        /// <item><term>Player disconnected</term><description>
        /// Dispose Peer to release resource.
        /// </description></item>
        /// </list>
        /// </summary>
        /// <param name="disconnect">Wherether player is disconnected or not</param>
        public void ResetPeer(bool disconnect)
        {
            if (disconnect)
            {
                Peer.Tell(PoisonPill.Instance);
            }
            Peer = null;
        }

        /// <summary>
        /// Convenient method to send a custom operation response. 
        /// customOpCode will be added to data payload to no conflict with Senla.Gamer's primary OpCode.
        /// </summary>
        /// <param name="customOpCode">A custom opCode, vary from 0 to 255</param>
        /// <param name="dataDict">Custom data</param>
        /// <param name="sendParameters">Parameters</param>
        public virtual void SendCustomOperationResponse(byte customOpCode, Dictionary<byte, object> dataDict, SendParameters sendParameters)
        {
            if (Peer != null)
            {
#if DEBUG
                if (dataDict.ContainsKey((byte)DataCode.EXTENDED_CODE))
                {
                    throw new UseReservedCodeException("One of parameters used OpCode entry to store data***");
                }
#endif
                dataDict.Add((byte)DataCode.EXTENDED_CODE, customOpCode);
                OperationData response = new OperationData(OperationCode.EXTENDED_OPERATION, dataDict);
                Peer.Tell(new OperationResponse(response, sendParameters));
                //if (Room != null)
                //{
                //    Room.LogTransmitData(response, Name);
                //}
            }
//#if DEBUG
//            else
//            {
//                dataDict.Add((byte)DataCode.EXTENDED_CODE, customOpCode);
//                OperationResponse operationResponse = new OperationResponse((byte)OperationCode.EXTENDED_OPERATION, dataDict);
//                OnReceiveResponse(operationResponse);
//            }
//#endif
        }

        /// <summary>
        /// Shortcut method to send operation response.
        /// </summary>
        /// <param name="operationResponse"></param>
        /// <param name="sendParameters"></param>
        public void SendOperationResponse(OperationData operationResponse, SendParameters sendParameters)
        {
            if (Peer != null)
            {
                Peer.Tell(new OperationResponse(operationResponse, sendParameters));
                //if (Room != null)
                //{
                //    Room.LogTransmitData(operationResponse, Name);
                //}
            }
//#if DEBUG
//            else
//            {
//                OnReceiveResponse(operationResponse);
//            }
//#endif
        }

        /// <summary>
        /// Convenient method to send a custom Event to this player. 
        /// customEventCode will be added to data payload to no conflict with Senla.Gamer's EventCode.
        /// </summary>
        /// <param name="customEvtCode"></param>
        /// <param name="dataDict"></param>
        /// <param name="sendParameters"></param>
        public virtual void SendCustomEvent(byte customEvtCode, Dictionary<byte, object> dataDict, SendParameters sendParameters)
        {
            if (Peer != null)
            {
#if DEBUG
                if (dataDict.ContainsKey((byte)DataCode.EXTENDED_CODE))
                {
                    throw new UseReservedCodeException("One of parameters used EvtCode entry to store data***");
                }
#endif
                dataDict.Add(DataCode.EXTENDED_CODE, customEvtCode);
                EventData response = new EventData((byte)EventCode.EXTENDED_EVENT, dataDict);
                Peer.Tell(new Event(response, sendParameters));
                //if (Room != null)
                //{
                //    Room.LogTransmitData(response, Name);
                //}
            }
//#if DEBUG
//            else
//            {
//                dataDict.Add((byte)DataCode.EXTENDED_CODE, customEvtCode);
//                EventData eventData = new EventData((byte)EventCode.EXTENDED_EVENT, dataDict);
//                OnReceiveEvent(eventData);
//            }
//#endif
        }

        /// <summary>
        /// Shortcut method to send event to this player.
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="sendParameters"></param>
        public void SendEvent(EventData eventData, SendParameters sendParameters)
        {
            if (Peer != null)
            {
                Peer.Tell(new Event(eventData, sendParameters));
                //if (Room != null)
                //{
                //    Room.LogTransmitData(eventData, Name);
                //}
            }

//#if DEBUG
//            else
//            {
//                OnReceiveEvent(eventData);
//            }
//#endif
        }

        /// <summary>
        /// Broadcast an event to all valid players in <code>players</code> list
        /// </summary>
        /// <param name="eventData">Data to send</param>
        /// <param name="players">List of players</param>
        /// <param name="sendParameters">Parameters</param>
        public static void BroadCastEvent(EventData eventData, IEnumerable<Player> players, SendParameters sendParameters)
        {
            var connectedPlayers = players.Where(p => p != null && p.Peer != null);

            foreach (var player in connectedPlayers)
            {
                player.SendEvent(eventData, sendParameters);
            }

            //var p2 = peers.Find(f => true);
            //if (p2 != null && p2.Player != null && p2.Player.Room != null)
            //{
            //    var names = string.Empty;
            //    foreach (var t in peers.Select(p => p.Player == null ? string.Empty : p.Player.Name))
            //    {
            //        names += " " + t;
            //    }
            //    p2.Player.Room.LogTransmitData(eventData, names);
            //}

//#if DEBUG
//            var bots = players.Where(p => p != null && p.Peer == null);
//            foreach (var bot in bots)
//            {
//                bot.OnReceiveEvent(eventData);
//            }
//#endif
        }

        /// <summary>
        /// Broadcast a custom event to all valid players in <code>players</code> list
        /// </summary>
        /// <param name="evtCode">Custom event code</param>
        /// <param name="data">Custom data in {DataCode: Data} pairs</param>
        /// <param name="players">List of players</param>
        public static void BroadCastCustomEvent(byte evtCode, Dictionary<byte, object> data, IEnumerable<Player> players, SendParameters sendParameters)
        {
#if DEBUG
            if (data.ContainsKey((byte)DataCode.EXTENDED_CODE))
            {
                throw new UseReservedCodeException("Some data is stored in CUSTOM_CODE entry");
            }
#endif
            data.Add((byte)DataCode.EXTENDED_CODE, evtCode);
            var eventData = new EventData((byte)EventCode.EXTENDED_EVENT, data);

            BroadCastEvent(eventData, players, sendParameters);

//            var p2 = peers.Find(f => true);
//            if (p2 != null && p2.Player != null && p2.Player.Room != null)
//            {
//                var names = string.Empty;
//                foreach (var t in peers.Select(p => p.Player == null ? string.Empty : p.Player.Name))
//                {
//                    names += " " + t;
//                }
//                p2.Player.Room.LogTransmitData(eventData, names);
//            }

//#if DEBUG
//            var bots = players.Where(p => p != null && p.Peer == null);
//            foreach (var bot in bots)
//            {
//                bot.OnReceiveEvent(eventData);
//            }
//#endif
        }

#if DEBUG
        public virtual void OnReceiveResponse(OperationData operationResponse)
        {
            if (OperationHandlers.ContainsKey(operationResponse.Code))
            {
                OperationHandlers[operationResponse.Code](operationResponse.Code, operationResponse);
            }
        }

        public virtual void OnReceiveEvent(EventData eventData)
        {
            if (EventHandlers.ContainsKey(eventData.Code))
            {
                EventHandlers[eventData.Code](eventData.Code, eventData);
            }
        }

        public delegate void OperationHandler(byte opCode, OperationData operationResponse);
        public delegate void EventHandler(byte opCode, EventData eventData);

        public Dictionary<byte, OperationHandler> OperationHandlers = new Dictionary<byte, OperationHandler>();
        public Dictionary<byte, EventHandler> EventHandlers = new Dictionary<byte, EventHandler>();

        public override string ToString()
        {
            return new StringBuilder(40)
                .Append(GetType().Name)
                .Append(", ")
                .Append(Name)
                .Append(", " + (IsBot ? "bot" : "playing"))
                .ToString();
        }
#endif
    }
}
