using Sen;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Interfaces
{
    public class LobbyState : RoomState, ILobbyState
    {
        public IList<IRoom> Rooms { get; set; }
    }
}
