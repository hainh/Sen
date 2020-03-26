using System;

namespace Senla.Server.SocketServer.WebSocket.Fleck
{
    public class HandshakeException : Exception
    {
        public HandshakeException() : base() { }
        
        public HandshakeException(string message) : base(message) {}
        
        public HandshakeException(string message, Exception innerException) : base(message, innerException) {}
    }
}

