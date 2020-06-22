using Sen;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Interfaces
{
    public class RoomState : IRoomState
    {
        public long MatchId { get; set; }
        public string RoomName { get; set; }
        public string Password { get; set; }
        public int PlayerLimit { get; set; }
        public ILobby Parent { get; set; }
        public IDictionary<string, IPlayer> Players { get; set; }
    }
}
