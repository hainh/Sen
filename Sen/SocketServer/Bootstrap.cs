using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Senla.Core;
using Senla.Server.Configuration;
using Senla.Server.SocketServer.TcpSocket;
using Senla.Server.SocketServer.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer
{
    public class Bootstrap
    {
        List<ServerBootstrap> _tcpServers;
        List<ServerBootstrap> _udpServers;
        List<IChannel> _tcpChannel;
        List<IChannel> _udpChannel;

        IEnumerable<IApplication> _applications;

        public Bootstrap()
        {
            _tcpServers = new List<ServerBootstrap>();
            _udpServers = new List<ServerBootstrap>();
            _tcpChannel = new List<IChannel>();
            _udpChannel = new List<IChannel>();
        }

        public async Task RunServer(IEnumerable<IApplication> apps, IEnumerable<Application> configs)
        {
            _applications = apps;

            var senlaSection = configs.First().CurrentConfiguration.GetSection("Senla") as SenlaSection;

            var bossBreakout = senlaSection.SocketThreadBreakoutIntervalMs;

            MultithreadEventLoopGroup bossGroup = new MultithreadEventLoopGroup(
                () => new SingleThreadEventLoop(null, TimeSpan.FromMilliseconds(bossBreakout)), 1);
            MultithreadEventLoopGroup workerGroup = bossGroup;

            if (!senlaSection.UseSocketThreadForHandlers)
            {
                var workerBreakout = senlaSection.HandlerThreadBreakoutIntervalMs;
                workerGroup = new MultithreadEventLoopGroup(
                    () => new SingleThreadEventLoop(null, TimeSpan.FromMilliseconds(workerBreakout)), 1);
            }

            try
            {
                foreach (Application appConfig in configs)
                {
                    var application = apps.Where(app => app.AppName == appConfig.Name).FirstOrDefault();
                    application.Setup();

                    await BindTcpServer(bossGroup, workerGroup, application, appConfig);
                    await BindWebSocketServer(bossGroup, workerGroup, application, appConfig);
                }
            }
            catch (Exception)
            {
                ShutdownServer();
            }
            finally
            {
            }
        }

        public void ShutdownServer()
        {
            List<Task> shutdownTasks = new List<Task>(2 * _tcpServers.Count + 2);
            foreach (var channel in _tcpChannel)
            {
                shutdownTasks.Add(channel.CloseAsync());
            }
            foreach (var channel in _udpChannel)
            {
                shutdownTasks.Add(channel.CloseAsync());
            }

            shutdownTasks.Add(_tcpServers[0].Group().ShutdownGracefullyAsync());
            shutdownTasks.Add(_tcpServers[0].ChildGroup().ShutdownGracefullyAsync());

            Task.WaitAll(shutdownTasks.ToArray());

            foreach (var app in _applications)
            {
                app.Shutdown();
            }
        }

        async Task BindTcpServer(IEventLoopGroup bossGroup, IEventLoopGroup workerGroup, IApplication application, Configuration.Application appConfig)
        {
            var tcpServer = new ServerBootstrap()
                        .Group(bossGroup, workerGroup)
                        .Channel<TcpServerSocketChannel>()
                        .Option(ChannelOption.SoBacklog, 100)
                        .Option(ChannelOption.TcpNodelay, true)
                        .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                            
                            pipeline.AddLast(new TcpSocketServerHandler());
                            pipeline.AddLast(new ReliableServerHandler(application, appConfig));
                            pipeline.AddLast(new ConfigClientHandler(appConfig.Settings));
                        }));

            var tcpChannel = await tcpServer.BindAsync(appConfig.Settings.TcpEndpoint.Endpoint);

            _tcpServers.Add(tcpServer);
            _tcpChannel.Add(tcpChannel);
        }

        async Task BindWebSocketServer(IEventLoopGroup bossGroup, IEventLoopGroup workerGroup, IApplication application, Configuration.Application appConfig)
        {
            var tcpServer = new ServerBootstrap()
                        .Group(bossGroup, workerGroup)
                        .Channel<TcpServerSocketChannel>()
                        .Option(ChannelOption.SoBacklog, 100)
                        .Option(ChannelOption.TcpNodelay, true)
                        .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;

                            pipeline.AddLast(new WebSocketServerHandler("ws", new string[0])
                            {
                                OnHandShakeCompleted = () =>
                                {
                                    pipeline.AddLast(new ReliableServerHandler(application, appConfig));
                                    pipeline.AddLast(new ConfigClientHandler(appConfig.Settings));
                                }
                            });
                        }));

            var tcpChannel = await tcpServer.BindAsync(appConfig.Settings.WebSocketEndpoint.Endpoint);

            _tcpServers.Add(tcpServer);
            _tcpChannel.Add(tcpChannel);
        }
    }
}
