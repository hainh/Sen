using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

using static DotNetty.Codecs.Http.HttpVersion;
using static DotNetty.Codecs.Http.HttpResponseStatus;
using Sen.OrleansInterfaces;
using System.Net;
using Orleans.Concurrency;
using Sen.Utilities.Concurrency;

namespace Sen.Proxy
{
    public class WebSocketServerHandler : SimpleChannelInboundHandler<object>
    {
        const string WebsocketPath = "/websocket";

        WebSocketServerHandshaker handshaker;

        IProxyConnection proxyConnection;

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            if (msg is IFullHttpRequest request)
            {
                this.HandleHttpRequest(ctx, request);
            }
            else if (msg is WebSocketFrame frame)
            {
                this.HandleWebSocketFrame(ctx, frame);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        void HandleHttpRequest(IChannelHandlerContext ctx, IFullHttpRequest req)
        {
            // Handle a bad request.
            if (!req.Result.IsSuccess)
            {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, BadRequest));
                return;
            }

            // Allow only GET methods.
            if (!Equals(req.Method, HttpMethod.Get))
            {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, Forbidden));
                return;
            }

            // Handshake
            var wsFactory = new WebSocketServerHandshakerFactory(
                GetWebSocketLocation(req), null, true, 5 * 1024 * 1024);
            this.handshaker = wsFactory.NewHandshaker(req);
            if (this.handshaker == null)
            {
                WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(ctx.Channel);
            }
            else
            {

                IPEndPoint remoteIpEndPoint = ctx.Channel.RemoteAddress as IPEndPoint;
                if (SenProxy.ProxyConfig.UseExternalProxy != UseExternalProxy.None)
                {
                    remoteIpEndPoint = GetRealRemoteEndPoint(req, remoteIpEndPoint);
                }
                //Create the grain to communicate with the server(silo)
                proxyConnection = SenProxy.OrleansClient.GetGrain<IProxyConnection>(Guid.NewGuid());
                proxyConnection.InitConnection(ctx.Channel.LocalAddress, remoteIpEndPoint);

                this.handshaker.HandshakeAsync(ctx.Channel, req);
            }
        }

        void HandleWebSocketFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            // Check for closing frame
            if (frame is CloseWebSocketFrame)
            {
                this.handshaker.CloseAsync(ctx.Channel, (CloseWebSocketFrame)frame.Retain());
                return;
            }

            if (frame is PingWebSocketFrame)
            {
                ctx.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
                return;
            }

            if (frame is BinaryWebSocketFrame)
            {
                ForwardDataToServer(ctx, frame);
                return;
            }
            if (frame is ContinuationWebSocketFrame)
            {
                Console.WriteLine($"Fragment frame {frame.IsFinalFragment} {frame.Content.ReadableBytes}");
                return;
            }

            // Not accept other frames, like TextWebSocketFrame
            ctx.CloseAsync();
        }

        void ForwardDataToServer(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            var buffer = new byte[frame.Content.ReadableBytes];
            frame.Content.ReadBytes(buffer);
            proxyConnection.Read(buffer.AsImmutable()).ContinueWith((Task<Immutable<byte[]>> task, object state) =>
            {
                if (task.Result.Value != null && state is IChannelHandlerContext ctx)
                {
                    var result = Unpooled.WrappedBuffer(task.Result.Value);
                    var f = new BinaryWebSocketFrame(result);
                    ctx.WriteAndFlushAsync(f);
                }
            }, ctx, TaskContinuationOptions.ExecuteSynchronously);
        }

        void EchoData(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            ctx.WriteAsync(frame.Retain());
        }

        static void SendHttpResponse(IChannelHandlerContext ctx, IFullHttpRequest req, IFullHttpResponse res)
        {
            // Generate an error page if response getStatus code is not OK (200).
            if (res.Status.Code != 200)
            {
                IByteBuffer buf = Unpooled.CopiedBuffer(Encoding.UTF8.GetBytes(res.Status.ToString()));
                res.Content.WriteBytes(buf);
                buf.Release();
                HttpUtil.SetContentLength(res, res.Content.ReadableBytes);
            }

            // Send the response and close the connection if necessary.
            Task task = ctx.Channel.WriteAndFlushAsync(res);
            if (!HttpUtil.IsKeepAlive(req) || res.Status.Code != 200)
            {
                task.ContinueWith((t, c) => ((IChannelHandlerContext)c).CloseAsync(),
                    ctx, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"{nameof(WebSocketServerHandler)} {e}");
            ctx.CloseAsync();
        }

        protected virtual string GetWebSocketLocation(IFullHttpRequest req)
        {
            bool result = req.Headers.TryGet(HttpHeaderNames.Host, out ICharSequence value);
            Debug.Assert(result, "Host header does not exist.");
            string location = value.ToString() + WebsocketPath;

            return "ws://" + location;
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            proxyConnection.Disconnect();
            base.ChannelInactive(context);
        }

        static IPEndPoint GetRealRemoteEndPoint(IFullHttpRequest req, IPEndPoint defaultEndPoint)
        {
            switch (SenProxy.ProxyConfig.UseExternalProxy)
            {
                //case UseExternalProxy.None:
                //    return defaultEndPoint;
                case UseExternalProxy.CloudFlare:
                    if (GetIPAddressInHeaders(req, "cf-connecting-ip", out IPAddress ip)
                        || GetIPAddressInHeaders(req, "true-client-ip", out ip))
                    {
                        return new IPEndPoint(ip, defaultEndPoint.Port);
                    }
                    break;
                default:
                    break;
            }
            //if (GetIPAddressInHeaders(req, "cf-connecting-ip", out IPAddress ip)
            //    || GetIPAddressInHeaders(req, "x-client-ip", out ip)
            //    || GetIPAddressInHeaders(req, "true-client-ip", out ip)
            //    || GetIPAddressInHeaders(req, "x-real-ip", out ip)
            //    || GetIPAddressInHeaders(req, "x-cluster-client-ip", out ip)
            //    || GetIPAddressInHeaders(req, "x-forwarded", out ip)
            //    || GetIPAddressInHeaders(req, "forwarded-for", out ip))
            //{
            //    return new IPEndPoint(ip, defaultEndPoint.Port);
            //}

            return defaultEndPoint;
        }

        static bool GetIPAddressInHeaders(IFullHttpRequest req, string name, out IPAddress ip)
        {
            var value = req.Headers.Get(new AsciiString(name), null);
            if (value != null && IPAddress.TryParse(value.ToString(), out ip))
            {
                return true;
            }
            ip = null;
            return false;
        }
    }
}
