using System;
using System.Collections.Generic;
using System.IO;

namespace Sen
{
    public class SenClient<TUnionData> : ISenClient where TUnionData : IUnionData
    {
        private string ipAddress;
        private int port;
        private readonly IMessageHandler messageHandler;
        private string username;
        private string password;
        private Protocol protocol;
        private bool authorized;

        private AbstractClient client;

        /// <summary>
        /// Socket connected but not fully authorized
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Is fully authorized
        /// </summary>
        public bool Authorized => authorized;

        /// <summary>
        /// Current connection state
        /// </summary>
        public ConnectionState ConnectionState { get; private set; }

        /// <summary>
        /// Current connection's protocol
        /// </summary>
        public Protocol Protocol => protocol;

        /// <summary>
        /// Current port to connect
        /// </summary>
        public int Port => port;

        /// <summary>
        /// Current ip address to connect
        /// </summary>
        public string IpAddress => ipAddress;

        private readonly Dictionary<uint, Action<TUnionData>> callbacks = new Dictionary<uint, Action<TUnionData>>();
        private uint rpcCounter = 0;

#if DEBUG
        private int dataCount = 0;
        private int messCount = 0;
        DateTime lastUpdated = DateTime.UtcNow;
        public bool EnablePerfCount = false;
#endif

        public SenClient(IMessageHandler messageHandler)
        {
            this.messageHandler = messageHandler;
        }

        public void Connect(Protocol protocol, string ipAddress, int port, string username, string password)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(username));
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(password));
            this.ipAddress = ipAddress;
            this.port = port;
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
            Connect(protocol, IpAddress, port, username, password);
        }

        public void SendAuthorityOnConnected()
        {
            client.Send(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(username + "@/$/#//" + password)));
        }

        public void OnStateChange(ConnectionState state)
        {
            ConnectionState = state;
            switch (state)
            {
                case ConnectionState.Connecting:
                    break;
                case ConnectionState.Connected:
                    Connected = true;
                    break;
                case ConnectionState.Authorized:
                    authorized = true;
                    break;
                case ConnectionState.Disconnected:
                    Connected = false;
                    authorized = false;
                    break;
                default:
                    break;
            }
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
                    OnStateChange(ConnectionState.Authorized);
                }
                return;
            }
            ushort serviceCode = (ushort)(buffer[data.Offset] | (buffer[data.Offset + 1] << 8));
            NetworkOptions options = new NetworkOptions();
            options.SetValues(serviceCode);
            if (options.MessageType == MessageType.Rpc)
            {
                var rpcMessage = MessagePack.MessagePackSerializer.Deserialize<RpcMessage<TUnionData>>(new ReadOnlyMemory<byte>(buffer, data.Offset + 2, data.Count - 2));
                if (callbacks.TryGetValue(rpcMessage.Id, out var callback))
                {
                    callback(rpcMessage.UnionData);
                    callbacks.Remove(rpcMessage.Id);
                }
                else
                {
#if NET_CLIENT
                    Console.WriteLine($"No rpc callback with Id: " + rpcMessage.Id);
#endif
#if UNITY
                    // Show warning here
#endif
                }
            }
            else
            {
                TUnionData message = MessagePack.MessagePackSerializer.Deserialize<TUnionData>(new ReadOnlyMemory<byte>(buffer, data.Offset + 2, data.Count - 2));
                messageHandler.DispatchMessage(message, options);
            }
        }

        readonly MemoryStream memory = new MemoryStream(256 * 1024);

        private void SendMessage<T>(T message, NetworkOptions options)
        {
            memory.SetLength(0);
            memory.Position = 0;
            ushort serviceCode = options.ToServiceCode();
            memory.WriteByte((byte)serviceCode);
            memory.WriteByte((byte)(serviceCode >> 8));
            MessagePack.MessagePackSerializer.Serialize(memory, message);
            client.Send(new ArraySegment<byte>(memory.ToArray()));
        }

        public void Send(TUnionData message, NetworkOptions options)
        {
            SendMessage(message, options);
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

        public void Send<T>(T message, NetworkOptions options, Action<TUnionData> callback)
            where T : TUnionData, IRpcMessage
        {
            if (options.MessageType != MessageType.Normal && options.MessageType != MessageType.Rpc)
            {
                throw new ArgumentException($"Not supported {nameof(NetworkOptions)}.{nameof(NetworkOptions.MessageType)} = {nameof(MessageType)}.{options.MessageType}");
            }
            callbacks[rpcCounter] = callback ?? throw new ArgumentNullException(nameof(callback));
            options.MessageType = MessageType.Rpc;
            var rpcMessage = new RpcMessage<TUnionData>() { Id = rpcCounter, UnionData = message };
            SendMessage(rpcMessage, options);
            
            rpcCounter++;
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
            if (client.Connected)
            {
                client.Disconnect();
            }
        }
    }
}
