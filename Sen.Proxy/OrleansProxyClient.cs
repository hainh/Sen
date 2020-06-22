using NLog.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Hosting;
using Orleans.Streams;
using Sen;
using Sen.Utilities.Console;
using System;
using System.Threading.Tasks;

namespace Sen.Proxy
{
    public class OrleansProxyClient<TPlayerGrain> : IPlayerFactory where TPlayerGrain : IPlayer, IGrainWithStringKey
    {
        public const string SMSProvider = "SMSProvider";

        public const string ProxyStream = "ProxyStream";

        public IClusterClient OrleansClusterClient { get; private set; }

        public IPlayer CreatePlayer(string playerId) => OrleansClusterClient.GetGrain<TPlayerGrain>(playerId);

        public IAsyncStream<Immutable<byte[]>> CreateStream(IPlayer player)
        {
            return OrleansClusterClient.GetStreamProvider(SMSProvider)
                .GetStream<Immutable<byte[]>>(Guid.NewGuid(), ProxyStream);
        }

        public async Task<int> RunOrleansProxyClient(ISenProxy senProxy, IConsoleCommand consoleCommand = null)
        {
            try
            {
                // Configure a client and connect to the service.
                OrleansClusterClient = new ClientBuilder()
                    .UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev")
                    .AddSimpleMessageStreamProvider(SMSProvider)
                    .ConfigureLogging(logging => logging.AddNLog())
                    .Build();

                senProxy.SetGrainFactory(this);
                await OrleansClusterClient.Connect(RetryFilter);
                Console.WriteLine("Proxy successfully connect to silo host");
                await senProxy.StartAsync();
                if (consoleCommand != null)
                {
                    await consoleCommand.RunAsync();
                }
                else
                {
                    while (true)
                    {
                        Console.ReadLine(); // Run till cancel key pressed
                    }
                }
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

        int attempt = 0;
        async Task<bool> RetryFilter(Exception exception)
        {
            attempt++;
            string msg = $"Cluster client attempt {attempt} failed to connect to cluster.  Exception: {exception.Message}";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
            await Task.Delay(TimeSpan.FromSeconds(10));
            return true;
        }
    }
}
