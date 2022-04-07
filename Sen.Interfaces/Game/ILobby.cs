using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sen
{
    public interface ILobby: IRoom
    {
        /// <summary>
        /// Get all room this looby contains
        /// </summary>
        /// <returns>List of all room this lobby contains</returns>
        ValueTask<IList<IRoom>> GetRooms();
        /// <summary>
        /// Find Room
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        ValueTask<IRoom?> FindRoom(string roomName);
        /// <summary>
        /// Add new Room
        /// </summary>
        /// <param name="room">Room to add</param>
        ValueTask AddNewRoom(IRoom room);
        /// <summary>
        /// Remove room
        /// </summary>
        /// <param name="room">Room to remove</param>
        /// <returns><code>true</code> if room exists and removed, <code>false</code> otherwise</returns>
        ValueTask<bool> RemoveRoom(IRoom room);
    }
}
