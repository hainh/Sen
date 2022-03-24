using System;

namespace Sen
{
    public interface ISenClient
    {
        void HandleData(ArraySegment<byte> data);
        void OnStateChange(ConnectionState state);
        void SendAuthorityOnConnected();
    }
}
