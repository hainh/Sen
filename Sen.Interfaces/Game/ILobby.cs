﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sen
{
    public interface ILobby: IRoom
    {
        //ValueTask<IList<IRoom>> Rooms { get; }
        ValueTask<IList<IRoom>> GetRooms();

        ValueTask<IRoom> FindRoom(string roomName);

        ValueTask AddNewRoom(IRoom room);

        ValueTask<bool> RemoveRoom(IRoom room);
    }
}
