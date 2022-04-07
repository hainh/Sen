using System;
using System.Collections.Generic;
using System.Text;

namespace Sen
{
    public interface IRoomState
    {
        long MatchId { get; set; }
        string RoomName { get; set; }
        string? Password { get; set; }
        int PlayerLimit { get; set; }
        ILobby? Parent { get; set; }
        IDictionary<string, IPlayer> Players { get; set; }
    }
}
