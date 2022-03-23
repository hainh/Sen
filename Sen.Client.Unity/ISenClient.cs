using Sen.Client.Unity.Abstract;
using System;

namespace Sen.Client.Unity
{
    public interface ISenClient
    {
        void HandleData(ArraySegment<byte> data);
        void OnStateChange(ConnectionState state);
        void SendAuthorityOnConnected();
    }
}
