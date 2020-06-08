using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sen.Game
{
    public interface ILobby: IRoom
    {
        ValueTask<IList<IRoom>> Rooms { get; }

        ValueTask<IRoom> FindRoom(string roomName);

        ValueTask AddNewRoom(IRoom room);

        ValueTask<bool> RemoveRoom(IRoom room);
    }
}
