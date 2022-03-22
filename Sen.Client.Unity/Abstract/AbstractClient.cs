using System;
using System.Collections.Generic;
using System.Text;

namespace Sen.Client.Unity.Abstract
{
    /// <summary>
    /// Create public void HandleMessage(TUnionData data, NetworkOption options) for each TUnionData type to handle those messages
    /// </summary>
    /// <typeparam name="TUnionData"></typeparam>
    public abstract class AbstractClient
    {
        protected readonly ISenClient senClient;

        public bool Connected { get; protected set; }
        public bool Connecting { get ; protected set; }

        public AbstractClient(ISenClient senClient)
        {
            this.senClient = senClient;
        }

        protected void OnConnected()
        {
            Connected = true;
            Connecting = false;
            senClient.OnStateChange(ConnectionState.Connected);
        }

        protected void OnDisconected()
        {
            Connected = false;
            senClient.OnStateChange(ConnectionState.Disconnected);
        }

        protected void OnData(ArraySegment<byte> data)
        {
            senClient.HandleData(data);
        }

        public abstract void Connect(string ipAddress, int port);

        public abstract void Send(ArraySegment<byte> data);

        public abstract void Disconnect();

        public abstract void Tick(int processLimit);
    }
}
