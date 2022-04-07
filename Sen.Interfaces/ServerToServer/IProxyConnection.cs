using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public interface IProxyConnection
    {
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
    }
}
