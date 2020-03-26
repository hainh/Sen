using Senla.Core.Buffer;
using Senla.Core.Heartbeat;
using Senla.Core.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public abstract class PeerBase
    {
        private ISocketPeer _socketPeer;

        private ISerializer _serializer;

        private IDeserializer _deserializer;

        public ISerializer Serializer { get { return _serializer; } }

        public IDeserializer Deserializer { get { return _deserializer; } }

        public bool Connected { get { return _socketPeer != null; } }

        public PeerBase(ISocketPeer socketPeer)
            : this (socketPeer, new DefaultSerializer(), new DefaultDeserializer())
        {
        }

        public PeerBase(ISocketPeer socketPeer, ISerializer serializer, IDeserializer deserializer)
        {
            _socketPeer = socketPeer;
            _serializer = serializer;
            _deserializer = deserializer;
        }

        public int ConnectionId
        {
            get
            {
                return _socketPeer?.ConnectionId ?? 0;
            }
        }

        public string LocalIp
        {
            get
            {
                return _socketPeer?.LocalIp ?? string.Empty;
            }
        }

        public int LocalPort
        {
            get
            {
                return _socketPeer?.LocalPort ?? -1;
            }
        }

        public string RemoteIp
        {
            get
            {
                return _socketPeer?.RemoteIp ?? string.Empty;
            }
        }

        public int RemotePort
        {
            get
            {
                return _socketPeer?.RemotePort ?? -1;
            }
        }

        public int PingTime { get; set; }

        public void Flush()
        {
            _socketPeer.Flush();
        }

        public abstract void OnDisconnect(DisconnectReason reason);

        public abstract void OnOperationRequest(OperationData operationData, SendParameters sendParameters);

        public bool Disconnected { get { return _socketPeer == null; } }

        private void disconnect()
        {
            _socketPeer.DisconnectAsync();
            _socketPeer = null;
        }

        public void Disconnect(DisconnectReason reason)
        {
            OnDisconnect(reason);
            disconnect();
        }

        public void Disconnect()
        {
            Disconnect(DisconnectReason.ServerDisconnect);
        }
        
        public void OnBufferFilled(IDataContainer dataObject, SendParameters sendParameters, uint error)
        {
            if (error > 0)
            {
                Disconnect(DisconnectReason.DataDeserializeFailed);
            }

            if (dataObject is OperationData)
            {
                OnOperationRequest(dataObject as OperationData, sendParameters);
            }
            else if (dataObject is Ping)
            {
                PingTime = (dataObject as Ping).GetPingTime();
            }
        }

        public void SendOperationResponse(OperationData operationResponse, SendParameters sendParameters)
        {
            if (Disconnected)
            {
                return;
            }

            sendData(operationResponse, sendParameters);
        }

        public void SendEvent(EventData data, SendParameters sendParameters)
        {
            if (Disconnected)
            {
                return;
            }

            sendData(data, sendParameters);
        }

        public static void BroadcastEvent(EventData data, IEnumerable<PeerBase> peers, SendParameters sendParameters)
        {
            foreach (PeerBase peer in peers.Where(p => p != null && p.Connected))
            {
                peer.SendEvent(data, sendParameters);
            }
        }

        internal void sendData(IDataContainer data, SendParameters sendParameters)
        {
            DequeBuffer<byte> rawBytes = sendParameters.Encrypted
                ? null
                : _serializer.Serialize(data);

            var message = new SendingDataWrapper(rawBytes, sendParameters);

            if (sendParameters.Flush)
            {
                _socketPeer.WriteAndFlushAsync(message);
            }
            else
            {
                _socketPeer.WriteAsync(message);
            }
        }

        internal void sendPing()
        {
            var ping = new Ping();
            ping.SetPingTime();
            ping.Parameters.Add(1, PingTime);

            sendData(ping, new SendParameters());
        }
    }
}
