using System;

namespace Sen
{
    internal class TelepathyClientTCP : AbstractClient
    {
        readonly Telepathy.Client rawClient;

        public TelepathyClientTCP(ISenClient senClient) : base(senClient)
        {
            rawClient = new Telepathy.Client(1024 * 1024);
            rawClient.OnConnected += OnConnected;
            rawClient.OnDisconnected += OnDisconected;
            rawClient.OnData += OnData;
        }

        public override void Connect(string ipAddress, int port)
        {
            Connecting = true;
            rawClient.Connect(ipAddress, port);
            senClient.OnStateChange(ConnectionState.Connecting);
        }

        public override void Disconnect()
        {
            Connected = false;
            Connecting = false;
            rawClient.Disconnect();
        }

        public override void Send(ArraySegment<byte> data)
        {
            rawClient.Send(data);
        }

        public override void Tick(int processLimit)
        {
            rawClient?.Tick(processLimit);
        }
    }
}
