using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NLog.Extensions.Logging;
using System.Runtime;

namespace Sen.Server
{
    public class SenServer
    {
        static SenServer()
        {
            Utilities.InternalLogger.LoggerFactory = new NLogLoggerFactory();
        }

        public static async Task<int> Run(Action<Microsoft.Extensions.Hosting.HostBuilderContext, ISiloBuilder> setupOrleans)
        {
            try
            {
                var host = new HostBuilder()
#if DEBUG
                    .UseEnvironment(Environments.Development)
#else
                    .UseEnvironment(Environments.Production)
#endif
                    .UseOrleans((context, siloBuilder) =>
                    {
                        setupOrleans(context, siloBuilder);
                    })
                    .ConfigureLogging(logging => logging.AddNLog())
                    .Build();

                HostManager.Host = host;
                Console.WriteLine($"Server garbage collection : {(GCSettings.IsServerGC ? "Enabled" : "Disabled")}");
                Console.WriteLine($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");

                await host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }
    }
}
