using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Sen;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static DotNetty.Codecs.Http.HttpResponseStatus;
using static DotNetty.Codecs.Http.HttpVersion;

namespace Sen.Proxy
{
    public class ClientObserverWs : IClientObserver
    {
        private readonly IChannelHandlerContext context;

        public ClientObserverWs(IChannelHandlerContext ctx)
        {
            context = ctx;
        }

        public void ReceiveData(Immutable<byte[]> data)
        {
            var result = Unpooled.WrappedBuffer(data.Value);
            var f = new BinaryWebSocketFrame(result);
            context.WriteAndFlushAsync(f);
        }
    }

    public class WebSocketServerHandler : SimpleChannelInboundHandler<object>
    {
        static readonly ILogger<WebSocketServerHandler> logger = Logger.LoggerFactory.CreateLogger<WebSocketServerHandler>();
        private readonly ProxyConfig proxyConfig;
        readonly IProxyServiceProvider proxyServiceProvider;
        const string WebsocketPath = "/websocket";
        WebSocketServerHandshaker? _handshaker;
        IProxyConnection? _proxyConnection;

        public WebSocketServerHandler(IProxyServiceProvider proxyServiceProvider, ProxyConfig proxyConfig)
        {
            this.proxyServiceProvider = proxyServiceProvider;
            this.proxyConfig = proxyConfig;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            if (msg is IFullHttpRequest request)
            {
                HandleHttpRequest(ctx, request);
            }
            else if (msg is WebSocketFrame frame)
            {
                HandleWebSocketFrame(ctx, frame);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        async void HandleHttpRequest(IChannelHandlerContext ctx, IFullHttpRequest req)
        {
            // Handle a bad request.
            if (!req.Result.IsSuccess)
            {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, BadRequest));
                return;
            }
            
            string[] uriComponent = req.Uri.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Allow only GET methods.
            if (uriComponent.Length != 3 || !Equals(req.Method, HttpMethod.Get) || uriComponent[0] != "websocket")
            {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, Forbidden));
                return;
            }

            // Handshake
            var wsFactory = new WebSocketServerHandshakerFactory(
                GetWebSocketLocation(req), null, true, 5 * 1024 * 1024);
            _handshaker = wsFactory.NewHandshaker(req);
            try
            {
                if (_handshaker == null)
                {
                    await WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(ctx.Channel);
                }
                else
                {
                    try
                    {
                        IAuthService authService = proxyServiceProvider.GetAuthServiceGrain();
                        if (!await authService.Login(uriComponent[0], uriComponent[1]))
                        {
                            await ctx.CloseAsync();
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, e.Message, Array.Empty<object>());
                        await ctx.CloseAsync();
                    }

                    if (ctx.Channel.RemoteAddress is not IPEndPoint remoteIpEndPoint || ctx.Channel.LocalAddress is not IPEndPoint local_IpEndPoint)
                    {
                        await ctx.CloseAsync();
                        return;
                    }

                    if (proxyConfig.UseExternalProxy != UseExternalProxy.None)
                    {
                        remoteIpEndPoint = GetRealRemoteEndPoint(req, remoteIpEndPoint);
                    }
                    bool authenticated;
                    try
                    {
                        IAuthService authService = proxyServiceProvider.GetAuthServiceGrain();
                        authenticated = await authService.Login(uriComponent[1], uriComponent[2]);
                        if (!authenticated)
                        {
                            await ctx.CloseAsync();
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, e.Message, Array.Empty<object>());
                        await ctx.CloseAsync();
                        return;
                    }

                    ClientObserverWs clientObserverWs = new(ctx);
                    //Create the grain to communicate with the server(silo)
                    IPlayer player = proxyServiceProvider.GetPlayer(uriComponent[0]);
                    _proxyConnection = player;
                    IClientObserver observer = await proxyServiceProvider.CreateObserver(clientObserverWs);
                    await player.InitConnection(local_IpEndPoint.Port, remoteIpEndPoint.Address.ToString(), observer);
                    if (authenticated)
                    {
                        await _handshaker.HandshakeAsync(ctx.Channel, req);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        void HandleWebSocketFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            // Check for closing frame
            if (frame is CloseWebSocketFrame)
            {
                _handshaker?.CloseAsync(ctx.Channel, (CloseWebSocketFrame)frame.Retain());
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

        async void ForwardDataToServer(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            try
            {
                if (_proxyConnection == null) return;
                var buffer = new byte[frame.Content.ReadableBytes];
                frame.Content.ReadBytes(buffer);
                var dataWriteBack = await _proxyConnection.OnReceivedData(buffer.AsImmutable());
                if (dataWriteBack.Value != null)
                {
                    var result = Unpooled.WrappedBuffer(dataWriteBack.Value);
                    var f = new BinaryWebSocketFrame(result);
                    await ctx.WriteAndFlushAsync(f);
                }
            }
            catch (Exception e)
            {
                logger.LogDebug(e, e.Message);
            }
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
            if (_proxyConnection != null)
            {
                _proxyConnection.OnDisconnect();
            }
            base.ChannelInactive(context);
        }

        IPEndPoint GetRealRemoteEndPoint(IFullHttpRequest req, IPEndPoint defaultEndPoint)
        {
            switch (proxyConfig.UseExternalProxy)
            {
                //case UseExternalProxy.None:
                //    return defaultEndPoint;
                case UseExternalProxy.CloudFlare:
                    if (TryGetIPAddressInHeaders(req, "cf-connecting-ip", out IPAddress? ip)
                        || TryGetIPAddressInHeaders(req, "true-client-ip", out ip))
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

        static bool TryGetIPAddressInHeaders(IFullHttpRequest req, string name, [NotNullWhen(returnValue: true)] out IPAddress? ip)
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
