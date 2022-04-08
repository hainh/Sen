using System;
using System.IO;

namespace Sen
{
    public class SenClient<TUnionData> : ISenClient where TUnionData : IUnionData
    {
        private readonly string ipAddress;
        private readonly int port;
        private readonly IMessageHandler messageHandler;
        private string username;
        private string password;
        private Protocol protocol;
        private bool authorized;

        private AbstractClient client;

#if DEBUG
        private int dataCount = 0;
        private int messCount = 0;
        DateTime lastUpdated = DateTime.UtcNow;
        public bool EnablePerfCount = false;
#endif

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
            this.protocol = protocol;
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

        public void Reconnect()
        {
            Connect(protocol, username, password);
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
#if DEBUG
            if (EnablePerfCount)
            {
                dataCount += data.Count;
                ++messCount;
            }
#endif
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
            NetworkOptions options = new NetworkOptions();
            options.SetValues(serviceCode);;
            var message = MessagePack.MessagePackSerializer.Deserialize<TUnionData>(new ReadOnlyMemory<byte>(buffer, data.Offset + 2, data.Count - 2));
            messageHandler.DispatchMessage(message, options);
        }

        readonly MemoryStream memory = new MemoryStream(256 * 1024);

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
#if DEBUG
            if (EnablePerfCount)
            {
                var span = (DateTime.UtcNow - lastUpdated).TotalSeconds;
                if (span >= 5)
                {
                    Console.WriteLine($"Data Speed {dataCount / span / 1024}KBps, {messCount / span}mps");
                    lastUpdated = DateTime.UtcNow;
                    messCount = 0;
                    dataCount = 0;
                }
            }
#endif
        }

        public void Disconnect()
        {
            client.Disconnect();
        }
    }
}
