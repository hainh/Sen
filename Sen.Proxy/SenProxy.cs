using DotNetty.Codecs.Http;
using DotNetty.Common;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using Orleans;
using System;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sen.Proxy
{
    public class SenProxy
    {
        public static IClusterClient OrleansClient { get; private set; }

        public static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            await Task.WhenAll(RunNettyServer(), RunOrleansProxyClient());
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

        }

        static async Task<int> RunOrleansProxyClient()
        {
            try
            {
                // Configure a client and connect to the service.
                OrleansClient = new ClientBuilder()
                    .UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev")
                    .ConfigureLogging(logging => logging.AddNLog())
                    .Build();

                await OrleansClient.Connect(RetryFilter);
                Console.WriteLine("Proxy successfully connect to silo host");
                Console.ReadKey();
                Console.WriteLine("Exitting Orleans");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        static int attempt = 0;
        static async Task<bool> RetryFilter(Exception exception)
        {
            attempt++;
            Console.WriteLine($"Cluster client attempt {attempt} failed to connect to cluster.  Exception: {exception}");
            await Task.Delay(TimeSpan.FromSeconds(10));
            return true;
        }

        static async Task RunNettyServer()
        {
            ILoggerFactory factory = new LoggerFactory();
            InternalLoggerFactory.DefaultFactory = new NLogLoggerFactory();
            ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Paranoid;
            Console.WriteLine($"Resource Leak Detector Level : {ResourceLeakDetector.Level}");
            Console.WriteLine($"Server garbage collection : {(GCSettings.IsServerGC ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");


            IEventLoopGroup bossGroup = new DispatcherEventLoopGroup();
            IEventLoopGroup workGroup = new WorkerEventLoopGroup((DispatcherEventLoopGroup)bossGroup);
        
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                        .Group(bossGroup, workGroup)
                        .Channel<TcpServerChannel>()
                        .Option(ChannelOption.SoBacklog, 8192)
                        .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                            pipeline.AddLast(new HttpServerCodec());
                            pipeline.AddLast(new HttpObjectAggregator(65536));
                            pipeline.AddLast(new WebSocketServerHandler());
                            
                        }));
                if (IsUnixLike())
                {
                    bootstrap
                            .Option(ChannelOption.SoReuseport, true)
                            .ChildOption(ChannelOption.SoReuseaddr, true);
                }

                IChannel bootstrapChannel = await bootstrap.BindAsync(IPAddress.Loopback, 9090);
                IChannel nextChannel = await bootstrap.BindAsync(IPAddress.Loopback, 9091);
                Console.WriteLine("Websocket listening");
                Console.ReadLine();
                await bootstrapChannel.CloseAsync();
                await nextChannel.CloseAsync();
                Console.WriteLine("Exited DotNetty");
            }
            finally
            {
                await workGroup.ShutdownGracefullyAsync();
                await bossGroup.ShutdownGracefullyAsync();
            }
        }

        static bool IsUnixLike()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }
    }
}
