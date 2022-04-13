using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using System;
using System.Net;
using System.Text;

namespace Sen.Proxy
{
    public class ClientObserverTcp : IClientObserver
    {
        private readonly IChannelHandlerContext context;

        public ClientObserverTcp(IChannelHandlerContext ctx)
        {
            context = ctx;
        }

        public void ReceiveData(Immutable<byte[]> data)
        {
            try
            {
                var result = Unpooled.WrappedBuffer(data.Value);
                context.WriteAndFlushAsync(result);
            }
            catch (Exception e)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(e, e.Message);
                }
            }
        }

        static readonly ILogger<TcpSocketServerHandler> logger = Logger.LoggerFactory.CreateLogger<TcpSocketServerHandler>();
    }

    public class TcpSocketServerHandler : ChannelHandlerAdapter
    {
        static readonly ILogger<TcpSocketServerHandler> logger = Logger.LoggerFactory.CreateLogger<TcpSocketServerHandler>();
        private readonly IProxyServiceProvider proxyServiceProvider;
        private readonly ProxyConfig _proxyConfig;
        IProxyConnection? _proxyConnection;
        bool authenticated;

        public TcpSocketServerHandler(IProxyServiceProvider proxyServiceProvider, ProxyConfig proxyConfig)
        {
            this.proxyServiceProvider = proxyServiceProvider;
            _proxyConfig = proxyConfig;
        }

        readonly byte[] discard4Bytes = new byte[4];
        public override async void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            try
            {
                if (message is not IByteBuffer cast)
                {
                    await ctx.CloseAsync();
                    return;
                }
                if (authenticated)
                {
                    var buffer = new byte[cast.ReadableBytes];
                    cast.ReadBytes(buffer);
                    var dataWriteBack = await _proxyConnection!.OnReceivedData(buffer.AsImmutable());
                    if (dataWriteBack.Value != null)
                    {
                        var result = Unpooled.WrappedBuffer(dataWriteBack.Value);
                        await ctx.WriteAndFlushAsync(result);
                    }
                }
                else // Authenticate...
                {
                    var connectionString = Encoding.UTF8.GetString(cast.Array, cast.ArrayOffset, cast.ReadableBytes);
                    var uriComponent = connectionString.Split("@/$/#//", 2);
                    if (uriComponent.Length != 2)
                    {
                        await ctx.CloseAsync();
                        return;
                    }
                    if (ctx.Channel.RemoteAddress is not IPEndPoint remoteIpEndPoint || ctx.Channel.LocalAddress is not IPEndPoint local_IpEndPoint)
                    {
                        await ctx.CloseAsync();
                        return;
                    }

                    if (uriComponent[0] == "@@LeafServer@@Hello") // Authenticate server2server
                    {
                        Listener? s2sListener = _proxyConfig.ServerToServerListeners.Length == 0 ? null : _proxyConfig.ServerToServerListeners[0];
                        if (s2sListener != null && s2sListener.Port == local_IpEndPoint.Port && local_IpEndPoint.Address.Equals(IPAddress.Loopback))
                        {
                            IServerToServerGrain s2sConnection = proxyServiceProvider.CreateServerToServerPeer(uriComponent[1]);
                            _proxyConnection = s2sConnection;
                            ClientObserverTcp clientObserverTcp = new(ctx);
                            IClientObserver observer = await proxyServiceProvider.CreateObserver(clientObserverTcp);
                            await s2sConnection.InitConnection("", observer);
                            authenticated = true;
                        }
                        else
                        {
                            await ctx.CloseAsync();
                            return;
                        }
                    }
                    else // Authenticates normal client
                    {
                        try
                        {
                            IAuthService authService = proxyServiceProvider.GetAuthServiceGrain();
                            authenticated = await authService.Login(uriComponent[0], uriComponent[1]);
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

                        ClientObserverTcp clientObserverTcp = new(ctx);
                        //Create the grain to communicate with the server(silo)
                        IPlayer player = proxyServiceProvider.GetPlayer(uriComponent[0]);
                        _proxyConnection = player;
                        IClientObserver observer = await proxyServiceProvider.CreateObserver(clientObserverTcp);
                        await player.InitConnection(local_IpEndPoint.Port, remoteIpEndPoint.Address.ToString(), observer);
                        await ctx.WriteAndFlushAsync(ctx.Allocator.Buffer(1).WriteByte(1));
                    }
                }
            }
            catch (Exception e)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(e, e.Message);
                }
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (_proxyConnection != null)
            {
                _proxyConnection.OnDisconnect();
                _proxyConnection = null;
            }
            base.ChannelInactive(context);
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"{nameof(WebSocketServerHandler)} {e}");
            ctx.CloseAsync();
        }
    }

    public static class TelepathyUtils
    {
        public static void IntToBytesBigEndianNonAlloc(int value, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }

        public static int BytesToIntBigEndian(byte[] bytes)
        {
            return (bytes[0] << 24) |
                   (bytes[1] << 16) |
                   (bytes[2] << 8) |
                    bytes[3];
        }
    }
}
