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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static DotNetty.Codecs.Http.HttpResponseStatus;
using static DotNetty.Codecs.Http.HttpVersion;

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
                byte[] payloadSize = new byte[4];
                TelepathyUtils.IntToBytesBigEndianNonAlloc(data.Value.Length, payloadSize);
                var result = Unpooled.WrappedBuffer(payloadSize, data.Value);
                context.WriteAndFlushAsync(result);
            }
            catch (Exception ex)
            {

            }
        }
    }

    public class TcpSocketServerHandler : ChannelHandlerAdapter
    {
        //protected static readonly log4net.ILog ioLogger = IOLogger.CreateLogger();
        //protected readonly ApplicationBase application;

        //private IPEndPoint remoteEndpoint;
        //private IPEndPoint localEndpoint;
        //private IChannelHandlerContext context;
        //private DisconnectReason disconnectReason = DisconnectReason.ServerDisconnect;
        //private readonly ISerializer serializer;
        //private readonly IDeserializer deserializer;
        //private PeerBase _peerBase;
        //private readonly QueueBuffer<byte> buffer;

        static readonly ILogger<TcpSocketServerHandler> logger = SenProxy.LoggerFactory.CreateLogger<TcpSocketServerHandler>();
        readonly IPlayerFactory _playerFactory;
        IPlayer _proxyConnection;

        public TcpSocketServerHandler(IPlayerFactory grainFactory)
        {
            _playerFactory = grainFactory;
        }

        bool handShaked = false;

        readonly byte[] discard4Bytes = new byte[4];
        public override async void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            try
            {
                IByteBuffer cast = message as IByteBuffer;
                if (handShaked)
                {
                    var buffer = new byte[cast.ReadableBytes - 4];
                    cast.ReadBytes(discard4Bytes);
                    cast.ReadBytes(buffer);
                    var dataWriteBack = await _proxyConnection.OnReceivedData(buffer.AsImmutable());
                    if (dataWriteBack.Value != null)
                    {
                        byte[] payloadSize = new byte[4];
                        TelepathyUtils.IntToBytesBigEndianNonAlloc(dataWriteBack.Value.Length, payloadSize);
                        var result = Unpooled.WrappedBuffer(payloadSize, dataWriteBack.Value);
                        await ctx.WriteAndFlushAsync(result);
                    }
                }
                else
                {
                    var connectionString = Encoding.UTF8.GetString(cast.Array, cast.ArrayOffset + 6, cast.ReadableBytes - 6);
                    var uriComponent = connectionString.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    IPEndPoint remoteIpEndPoint = ctx.Channel.RemoteAddress as IPEndPoint;
                    ClientObserverTcp clientObserverTcp = new(ctx);
                    //Create the grain to communicate with the server(silo)
                    _proxyConnection = _playerFactory.CreatePlayer(uriComponent[0]);
                    IClientObserver observer = await _playerFactory.CreateObserver(clientObserverTcp);
                    handShaked = await _proxyConnection.InitConnection(ctx.Channel.LocalAddress.ToString(), remoteIpEndPoint.ToString(),
                            username: uriComponent[0], accessToken: uriComponent[1], observer);
                    if (!handShaked)
                    {
                        await ctx.CloseAsync();
                    }
                }
            }
            catch (Exception e)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(e, e.Message);
                }
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (_proxyConnection != null)
            {
                _proxyConnection.Disconnect();
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
