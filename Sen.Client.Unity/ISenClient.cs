using Sen.Client.Unity.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sen.Client.Unity
{
    public interface ISenClient
    {
        void HandleData(ArraySegment<byte> data);
        void OnStateChange(ConnectionState state);
    }
}
