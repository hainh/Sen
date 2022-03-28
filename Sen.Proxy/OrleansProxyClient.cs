using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Hosting;
using Sen.Utilities.Console;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Sen.Proxy
{
    public class OrleansProxyClient<TPlayerGrain> : IPlayerFactory where TPlayerGrain : IPlayer, IGrainWithStringKey
    {
        static readonly ILogger<OrleansProxyClient<TPlayerGrain>> logger = SenProxy.LoggerFactory.CreateLogger<OrleansProxyClient<TPlayerGrain>>();
        public const string SMSProvider = "SMSProvider";

        public const string ProxyStream = "ProxyStream";

        public IClusterClient OrleansClusterClient { get; private set; }

        public IPlayer CreatePlayer(string playerId) => OrleansClusterClient.GetGrain<TPlayerGrain>(playerId);

        public bool Connected { get; private set; }

        Task<IClientObserver> IPlayerFactory.CreateObserver<T>(T observer)
        {
            return OrleansClusterClient.CreateObjectReference<IClientObserver>(observer);
        }

        public async Task<int> RunOrleansProxyClient(ISenProxy senProxy, IConsoleCommand consoleCommand = null)
        {
            try
            {
                // Configure a client and connect to the service.
                OrleansClusterClient = new ClientBuilder()
                    .UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev")
                    .ConfigureLogging(logging => logging.AddNLog())
                    .Build();

                senProxy.SetGrainFactory(this);
                await OrleansClusterClient.Connect(RetryFilter);
                Connected = true;
                string message = "Proxy successfully connect to silo host";
                Console.WriteLine(message);
                logger.LogInformation(message);
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
                message = "Exitting Orleans";
                Console.WriteLine(message);
                logger.LogInformation(message);
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
            logger.LogInformation(msg);
            await Task.Delay(TimeSpan.FromSeconds(10));
            return true;
        }
    }
}
