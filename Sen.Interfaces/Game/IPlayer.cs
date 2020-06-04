﻿using Orleans;
using Orleans.Concurrency;
using Sen.DataModel;
using Sen.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Game
{
    public interface IPlayer
    {
        /// <summary>
        /// Get room grain id
        /// </summary>
        ValueTask<IRoom> Room { get; }
        /// <summary>
        /// Set room grain id
        /// </summary>
        /// <param name="room">Room grain</param>
        ValueTask SetRoom(IRoom room);
        /// <summary>
        /// Is this player disconnected and became a bot
        /// </summary>
        ValueTask<bool> IsBot { get; }
        /// <summary>
        /// Start the connection
        /// </summary>
        /// <param name="local">Local IP endpoint</param>
        /// <param name="remote">Remote IP endpoint</param>
        /// <param name="accessToken">Access token for authorization</param>
        /// <returns>true if successfully authorized, false other wise</returns>
        ValueTask<bool> InitConnection(EndPoint local, EndPoint remote, string accessToken);
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
