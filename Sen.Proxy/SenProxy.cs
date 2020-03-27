using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Net;
using System.Runtime;
using System.Threading.Tasks;

using static DotNetty.Codecs.Http.HttpVersion;
using static DotNetty.Codecs.Http.HttpResponseStatus;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using System.Diagnostics;
using Orleans;
using Sen.OrleansInterfaces;
using Microsoft.Extensions.Logging;

namespace Sen.Proxy
{
    public class SenProxy
    {
        public static async Task Main()
        {
            await Task.WhenAny(RunServer(), RunOrleans());
        }

        static async Task<int> RunOrleans()
        {
            try
            {
                // Configure a client and connect to the service.
                var client = new ClientBuilder()
                    .UseLocalhostClustering(serviceId: "HelloWorldApp", clusterId: "dev")
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();

                await client.Connect(CreateRetryFilter());
                Console.WriteLine("Client successfully connect to silo host");

                // Use the connected client to call a grain, writing the result to the terminal.
                var friend = client.GetGrain<IProxyConnection>(0);
                var response = await friend.Test("Good morning, my friend!");
                Console.WriteLine("\n\n{0}\n\n", response);

                Console.ReadKey();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static Func<Exception, Task<bool>> CreateRetryFilter(int maxAttempts = 5)
        {
            var attempt = 0;
            return RetryFilter;

            async Task<bool> RetryFilter(Exception exception)
            {
                attempt++;
                Console.WriteLine($"Cluster client attempt {attempt} of {maxAttempts} failed to connect to cluster.  Exception: {exception}");
                if (attempt > maxAttempts)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(4));
                return true;
            }
        }

        static async Task RunServer()
        {
            ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Paranoid;
            Console.WriteLine($"Server garbage collection : {(GCSettings.IsServerGC ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");

            IEventLoopGroup bossGroup = new DispatcherEventLoopGroup();
            IEventLoopGroup workGroup = new WorkerEventLoopGroup((DispatcherEventLoopGroup)bossGroup);
        
            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(bossGroup, workGroup);

                bootstrap.Channel<TcpServerChannel>();

                bootstrap
                        .Option(ChannelOption.SoBacklog, 8192)
                        .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                            pipeline.AddLast(new HttpServerCodec());
                            pipeline.AddLast(new HttpObjectAggregator(65536));
                            pipeline.AddLast(new WebSocketServerHandler());
                            
                        }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(IPAddress.Loopback, 9090);
                IChannel nextChannel = await bootstrap.BindAsync(IPAddress.Loopback, 9091);
                Console.WriteLine("Websocket listening");
                Console.ReadLine();
                await bootstrapChannel.CloseAsync();
                await nextChannel.CloseAsync();
            }
            finally
            {
                await workGroup.ShutdownGracefullyAsync();
                await bossGroup.ShutdownGracefullyAsync();
            }
        }
    }
}
