using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using Orleans;
using Sen.Game;
using Sen.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Proxy
{
    public class OrleansProxyClient<TPlayer> where TPlayer : IPlayer, IGrainWithStringKey
    {
        public IClusterClient OrleansClusterClient { get; private set; }

        protected virtual IPlayer GetPlayer(string playerId) => OrleansClusterClient.GetGrain<TPlayer>(playerId);

        public async Task<int> RunOrleansProxyClient(ISenProxy senProxy)
        {
            try
            {
                // Configure a client and connect to the service.
                OrleansClusterClient = new ClientBuilder()
                    .UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev")
                    .ConfigureLogging(logging => logging.AddNLog())
                    .Build();

                senProxy.SetGrainFactory(GetPlayer);
                await OrleansClusterClient.Connect(RetryFilter);
                Console.WriteLine("Proxy successfully connect to silo host");
                await senProxy.WaitForShutdownAsync();
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
            string msg = $"Cluster client attempt {attempt} failed to connect to cluster.  Exception: {exception}";
            Console.WriteLine(msg);
            await Task.Delay(TimeSpan.FromSeconds(10));
            return true;
        }
    }
}
