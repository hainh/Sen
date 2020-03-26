using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Senla.Core;
using Senla.Core.Buffer;
using Senla.Core.Heartbeat;
using Senla.Core.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer
{
    public class ReliableServerHandler : ChannelHandlerAdapter
    {
        protected IApplication _application;

        protected Configuration.Application _appConfig;

        internal protected PeerBase _peer;

        protected Dictionary<byte, QueueBuffer<byte>> _buffers;

        protected HeartbeatHandler _heartbeatHandler;

        protected internal IChannelHandlerContext Context;

        public ReliableServerHandler(IApplication app, Configuration.Application appConfig)
        {
            _application = app;
            _appConfig = appConfig;
            _buffers = new Dictionary<byte, QueueBuffer<byte>>(2)
            {
                { 0, new QueueBuffer<byte>(256) }
            };
        }

        public override void HandlerAdded(IChannelHandlerContext context)
        {
            base.HandlerAdded(context);
            Context = context;
            _peer = _application.CreatePeer(new SocketPeer(context));
            _heartbeatHandler = new HeartbeatHandler(this, _appConfig.Settings.TcpMinIdleTime, _appConfig.Settings.TcpMaxIdleTime);

            _heartbeatHandler.UpdateHeartbeat();
            _heartbeatHandler.Start();
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is ReceivedDataWrapper)
            {
                var receivedDataWrapper = (ReceivedDataWrapper)message;
                var socketBuffer = receivedDataWrapper.Buffer;
                var channelId = receivedDataWrapper.SendParameters.ChannelId;

                QueueBuffer<byte> buffer = _buffers[channelId];

                if (buffer == null)
                {
                    buffer = new QueueBuffer<byte>(96);
                    _buffers.Add(channelId, buffer);
                }

                if (socketBuffer.HasArray)
                {
                    buffer.Enqueue(socketBuffer.Array, socketBuffer.ReaderIndex, socketBuffer.ReadableBytes);
                }
                else
                {
                    buffer.Enqueue(socketBuffer.ToArray());
                }

                try
                {
                    var dataContainer = _peer.Deserializer.DeserializeData(buffer);
                    if (dataContainer != null)
                    {
                        _peer.OnBufferFilled(dataContainer, receivedDataWrapper.SendParameters, 0);
                    }
                }
                catch (Exception)
                {
                    _peer.OnBufferFilled(null, null, 1);
                }
                
                socketBuffer.Release();
            }

            _heartbeatHandler.UpdateHeartbeat();
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
            context.FireChannelReadComplete();
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            // just forward message to underly handlers
            return context.WriteAsync(message);
        }

        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            if (!_peer.Disconnected)
            {
                _peer.Disconnect(DisconnectReason.ConnectionLost);
            }
            _heartbeatHandler.Disconnected();

            _heartbeatHandler = null;
            _peer = null;
            _appConfig = null;
            _application = null;
            _buffers = null;
            Context = null;
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);
        }

        /// <summary>
        /// Forwards disconnection to peer.
        /// Call this method as timeout detected by heartbeat
        /// </summary>
        public void ChannelIdleTimeout()
        {
            if (!_peer.Disconnected)
            {
                _peer.Disconnect(DisconnectReason.ConnectionTimeout);
            }
        }
    }
}
