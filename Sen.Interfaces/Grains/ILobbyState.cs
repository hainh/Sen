using System;
using System.Collections.Generic;
using System.Text;

namespace Sen
{
    public interface ILobbyState : IRoomState
    {
        IList<IRoom>? Rooms { get; set; }
    }
}
