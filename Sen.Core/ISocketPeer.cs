using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public interface ISocketPeer
    {
        int ConnectionId { get; }
        string LocalIp { get; }
        string RemoteIp { get; }
        int LocalPort { get; }
        int RemotePort { get; }

        void Flush();
        void DisconnectAsync();
        void WriteAsync(object message);
        void WriteAndFlushAsync(object message);
    }
}
