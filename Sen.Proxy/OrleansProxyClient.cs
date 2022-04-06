using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Sen.Extension;
using Sen.Utilities.Console;
using System;
using System.Threading.Tasks;

namespace Sen.Proxy
{
    public class OrleansProxyClient<TPlayerGrain, TServerToServerGrain> : IProxyServiceProvider
        where TPlayerGrain : IPlayer, IGrainWithStringKey
        where TServerToServerGrain : IServerToServerGrain, IGrainWithStringKey
    {
        static readonly ILogger<OrleansProxyClient<TPlayerGrain, TServerToServerGrain>> logger
            = Logger.LoggerFactory.CreateLogger<OrleansProxyClient<TPlayerGrain, TServerToServerGrain>>();

        public IClusterClient OrleansClusterClient { get; private set; }

        IPlayer IProxyServiceProvider.GetPlayer(string playerId) => OrleansClusterClient.GetGrain<TPlayerGrain>(playerId);

        IServerToServerGrain IProxyServiceProvider.CreateServerToServerPeer(string leafServerId)
            => OrleansClusterClient.GetGrain<TServerToServerGrain>(leafServerId);

        IAuthService IProxyServiceProvider.GetAuthServiceGrain() => OrleansClusterClient.GetGrain<IAuthService>();

        public bool Connected { get; private set; }

        Task<IClientObserver> IProxyServiceProvider.CreateObserver<T>(T observer)
        {
            return OrleansClusterClient.CreateObjectReference<IClientObserver>(observer);
        }

        public async Task<int> RunOrleansProxyClient(ISenProxy senProxy, Action<IClientBuilder> configClient, IConsoleCommand consoleCommand = null)
        {
            try
            {
                // Configure a client and connect to the service.
                var builder = new ClientBuilder()
                    .ConfigureLogging(logging => logging.AddNLog());
                configClient(builder);
                OrleansClusterClient = builder.Build();

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
                logger.LogError(e.ToString());
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
