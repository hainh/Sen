using Orleans.Concurrency;
using Sen;
using System.Net;
using System.Threading.Tasks;

namespace Sen
{
    public interface IPlayer
    {
        /// <summary>
        /// Get Player's ID (name)
        /// </summary>
        ValueTask<string> GetName();
        /// <summary>
        /// Get room grain id
        /// </summary>
        //ValueTask<IRoom> Room { get; }
        ValueTask<IRoom> GetRoom();
        /// <summary>
        /// Set Room this player is living
        /// </summary>
        /// <param name="room">Room this player'd joined</param>
        ValueTask SetRoomJoined(IRoom room);
        /// <summary>
        /// Is this player disconnected and became a bot
        /// </summary>
        //ValueTask<bool> IsBot { get; }
        ValueTask<bool> IsBot();
        /// <summary>
        /// Start the connection. Called by client or proxy server to initialize connection infomation.
        /// </summary>
        /// <param name="local">Local IP endpoint</param>
        /// <param name="remote">Remote IP endpoint</param>
        /// <param name="username">Username for authorization</param>
        /// <param name="accessToken">Access token for authorization</param>
        /// <returns>true if successfully authorized, false other wise</returns>
        ValueTask<bool> InitConnection(EndPoint local, EndPoint remote, string username, string accessToken);
        /// <summary>
        /// Write/send data to client
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="underlieData">End user's data representation</param>
        /// <returns>ValueTask to wait</returns>
        ValueTask Write(Immutable<IUnionData> data, WiredDataType underlieData = WiredDataType.Normal);
        /// <summary>
        /// Read data from client
        /// </summary>
        /// <param name="data">Data recieved</param>
        /// <returns>Data to write back to client</returns>
        ValueTask<Immutable<byte[]>> Read(Immutable<byte[]> data); // Read data from client
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
