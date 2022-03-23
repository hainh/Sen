using Sen.Client.Unity.Abstract;
using System;
using System.IO;

namespace Sen.Client.Unity
{
    public class SenClient<TUnionData> : ISenClient where TUnionData : IUnionData
    {
        private readonly string ipAddress;
        private readonly int port;
        private readonly IMessageHandler messageHandler;
        private string username;
        private string password;
        private bool authorized;

        private AbstractClient client;

        public SenClient(string ipAddress, int port, IMessageHandler messageHandler)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.messageHandler = messageHandler;
        }

        public void Connect(Protocol protocol, string username, string password)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(username));
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(password));
            this.username = username;
            this.password = password;
            switch (protocol)
            {
                case Protocol.Tcp:
                    if (client != null && client.Connected)
                    {
                        client.Disconnect();
                    }
                    client = new TelepathyClientTCP(this);
                    client.Connect(ipAddress, port);
                    break;
                default:
                    throw new NotSupportedException($"Protocol {protocol} is not supported");
            }
        }

        public void SendAuthorityOnConnected()
        {
            client.Send(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(username + "@/$/#//" + password)));
        }

        public void OnStateChange(ConnectionState state)
        {
            messageHandler.OnStateChange(state);
        }

        void ISenClient.HandleData(ArraySegment<byte> data)
        {
            byte[] buffer = data.Array;
            if (!authorized)
            {
                if (data.Count == 1 && buffer[data.Offset] == 1)
                {
                    authorized = true;
                    OnStateChange(ConnectionState.Authorized);
                }
                return;
            }
            ushort serviceCode = (ushort)(buffer[data.Offset] | (buffer[data.Offset + 1] << 8));
            NetworkOptions options = new();
            options.SetValues(serviceCode);;
            var message = MessagePack.MessagePackSerializer.Deserialize<TUnionData>(new ReadOnlyMemory<byte>(buffer, data.Offset + 2, data.Count - 2));
            messageHandler.HandleMessage(message, options);
        }

        readonly MemoryStream memory = new(256 * 1024);

        public void Send(TUnionData message, NetworkOptions options)
        {
            memory.SetLength(0);
            memory.Position = 0;
            ushort serviceCode = options.ToServiceCode();
            memory.WriteByte((byte)serviceCode);
            memory.WriteByte((byte)(serviceCode >> 8));
            MessagePack.MessagePackSerializer.Serialize(memory, message);
            client.Send(new ArraySegment<byte>(memory.ToArray()));
        }

        public void Send(ArraySegment<byte> rawData, NetworkOptions options)
        {
            memory.SetLength(0);
            memory.Position = 0;
            ushort serviceCode = options.ToServiceCode();
            memory.WriteByte((byte)serviceCode);
            memory.WriteByte((byte)(serviceCode >> 8));
            memory.Write(rawData.Array, rawData.Offset, rawData.Count);
            client.Send(new ArraySegment<byte>(memory.ToArray()));
        }

        public void Tick(int processLimit)
        {
            client.Tick(processLimit);
        }

        public void Disconnect()
        {
            client.Disconnect();
        }
    }
}
