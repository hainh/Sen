using System;

namespace Senla.Server.SocketServer.WebSocket.Fleck
{
    public interface IWebSocketServer : IDisposable
    {
        void Start(Action<IWebSocketConnection> config);
    }
}
