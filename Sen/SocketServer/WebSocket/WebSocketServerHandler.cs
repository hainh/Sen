using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Senla.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer.WebSocket
{
    using Core;
    using Fleck;
    using System.Net;
    public class WebSocketServerHandler : ChannelHandlerAdapter
    {
        protected IHandler WebSockerHandler { get; set; }

        public IWebSocketConnectionInfo ConnectionInfo { get; private set; }

        protected IEnumerable<string> SupportedSubProtocols { get; set; }
        /// <summary>
        /// Websocket scheme: ws or wss
        /// </summary>
        public string Scheme { get; private set; }

        public Action OnHandShakeCompleted { get; set; }

        protected QueueBuffer<byte> _buffer;

        protected IChannelHandlerContext Context;

        bool _pingedToNextHandler = false;

        public WebSocketServerHandler(string scheme, IEnumerable<string> supportedSubProtocols)
        {
            Scheme = scheme;
            SupportedSubProtocols = supportedSubProtocols;

            _buffer = new QueueBuffer<byte>();
        }

        protected void CreateWsHandler(IChannelHandlerContext context)
        {
            WebSocketHttpRequest request = RequestParser.Parse(_buffer.ToArray(), Scheme);

            if (request != null)
            {
                WebSockerHandler = HandlerFactory.BuildHandler(request, OnMessage, OnClose, OnBinary, OnPing, OnPong);
                
                if (WebSockerHandler == null)
                {
                    OnClose();
                    return;
                }

                string subProtocol = SubProtocolNegotiator.Negotiate(SupportedSubProtocols, request.SubProtocols);
                ConnectionInfo = WebSocketConnectionInfo.Create(request,
                    (context.Channel.RemoteAddress as IPEndPoint).Address.ToString(),
                    (context.Channel.RemoteAddress as IPEndPoint).Port,
                    subProtocol);

                byte[] handshake = WebSockerHandler.CreateHandshake(subProtocol);
                context.WriteAndFlushAsync(Unpooled.WrappedBuffer(handshake));

                OnHandShakeCompleted();
            }
        }

        protected void OnPong(byte[] obj)
        {
            Console.WriteLine("On Pong websocket");
        }

        protected void OnPing(byte[] obj)
        {
            Context.FireChannelRead(new Senla.Core.Heartbeat.Ping());
        }

        protected void OnBinary(byte[] binary)
        {
            _pingedToNextHandler = true;
            var sendParameters = new SendParameters();
            Context.FireChannelRead(new ReceivedDataWrapper(Unpooled.WrappedBuffer(binary), sendParameters));
        }

        protected void OnClose()
        {
            Context.CloseAsync();
        }

        protected void OnMessage(string @string)
        {
            Console.WriteLine("Not support text protoccol\r\n\tMessage: {0}", @string);
            @string = "Echo: " + @string;
            var raw = WebSockerHandler.FrameText(@string);
            Context.WriteAsync(Unpooled.WrappedBuffer(raw));
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Context = context;
            base.ChannelActive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var byteBuffer = (UnpooledHeapByteBuffer)message;
            _buffer.Clear();
            _buffer.Enqueue(byteBuffer.Array, byteBuffer.ReaderIndex, byteBuffer.ReadableBytes);

            if (WebSockerHandler == null)
            {
                CreateWsHandler(context);
            }
            else
            {
                WebSockerHandler.Receive(_buffer);

                if (!_pingedToNextHandler)
                {
                    context.FireChannelRead(new Senla.Core.Heartbeat.Ping());
                }
                _pingedToNextHandler = false;
            }
            byteBuffer.Release();
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            if (WebSockerHandler != null)
            {
                context.FireChannelReadComplete();
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            context.Channel.CloseAsync();
            base.ChannelInactive(context);
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            var messageRaw = ((message as SendingDataWrapper).Data as QueueBuffer<byte>).ToArray();
            var wsData = WebSockerHandler.FrameBinary(messageRaw);

            var buffer = context.Allocator.Buffer(wsData.Length);
            buffer.WriteBytes(wsData, 0, wsData.Length);
            return context.WriteAsync(buffer);

            //return context.WriteAsync(Unpooled.WrappedBuffer(wsData));
            //return context.WriteAsync(Unpooled.WrappedBuffer(WebSockerHandler.FrameBinary(new byte[1])));
        }
    }
}
