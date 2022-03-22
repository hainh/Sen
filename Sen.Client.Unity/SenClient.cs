using Sen.Client.Unity.Abstract;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sen.Client.Unity
{
    /// <summary>
    /// Implement public void HandleMessage(TUnionData data, NetworkOption options) for each TUnionData type to handle those messages
    /// </summary>
    public abstract class SenClient<TUnionData> : ISenClient where TUnionData : IUnionData
    {
        private readonly string ipAddress;
        private readonly int port;

        private AbstractClient client;

        public SenClient(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        public void Connect(Protocol protocol)
        {
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

        public abstract void OnStateChange(ConnectionState state);

        void ISenClient.HandleData(ArraySegment<byte> data)
        {
            byte[] buffer = data.Array;
            ushort serviceCode = (ushort)(buffer[data.Offset] | (buffer[data.Offset + 1] << 8));
            NetworkOptions options = new();
            options.SetValues(serviceCode);
            dynamic message = MessagePack.MessagePackSerializer.Deserialize<TUnionData>(data.AsMemory().Slice(2, data.Count - 2));
            ((dynamic)this).HandleMessage(message, options);
        }

        /// <summary>
        /// Sample handler, this object is calling <c>`((dynamic)this).HandleMessage(message, options);`</c>
        /// to resolve HandleMessage on runtime
        /// </summary>
        public void HandleMessage(IUnionData data, NetworkOptions options)
        {
            Console.WriteLine("No HandleMessage for " + data.GetType().Name);
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
