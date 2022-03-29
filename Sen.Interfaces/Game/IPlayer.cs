using Orleans.Concurrency;
using System.Net;
using System.Threading.Tasks;

namespace Sen
{
    public interface IPlayer : Orleans.IGrainWithStringKey
    {
        /// <summary>
        /// Get Player's ID (name)
        /// </summary>
        ValueTask<string> GetName();
        /// <summary>
        /// Get current room
        /// </summary>
        ValueTask<IRoom> GetRoom();
        /// <summary>
        /// Set Room this player is living
        /// </summary>
        /// <param name="room">Room this player'd joined</param>
        /// <returns>true if joined</returns>
        ValueTask<bool> JoinRoom(IRoom room);
        /// <summary>
        /// Leave current room
        /// </summary>
        /// <returns>true if has room to leave</returns>
        ValueTask<bool> LeaveRoom();
        /// <summary>
        /// Is this player disconnected and became a bot
        /// </summary>
        ValueTask<bool> IsBot();
        /// <summary>
        /// Start the connection. Called by client or proxy server to initialize connection infomation.
        /// </summary>
        /// <param name="local">Local IP endpoint</param>
        /// <param name="remote">Remote IP endpoint</param>
        /// <param name="username">Username for authorization</param>
        /// <param name="accessToken">Access token for authorization</param>
        /// <param name="observer">Observer to receive data from server</param>
        /// <returns>true if successfully authorized, false other wise</returns>
        ValueTask<bool> InitConnection(string local, string remote, string username, string accessToken, IClientObserver observer);
        /// <summary>
        /// Write/send data to client
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="underlieData">End user's data representation</param>
        /// <returns>ValueTask to wait</returns>
        ValueTask SendData(Immutable<IUnionData> data, NetworkOptions networkOptions);
        /// <summary>
        /// Write/send data to client
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        ValueTask SendData(Immutable<byte[]> data);
        /// <summary>
        /// Read data from client
        /// </summary>
        /// <param name="data">Data recieved</param>
        /// <returns>Data to write back to client</returns>
        ValueTask<Immutable<byte[]>> OnReceivedData(Immutable<byte[]> data); // Read data from client
        /// <summary>
        /// Raises on connection closed
        /// </summary>
        ValueTask OnDisconnect();
        /// <summary>
        /// Call this method to close the connection
        /// </summary>
        ValueTask Disconnect();
    }
}
