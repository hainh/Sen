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
    class SenServer
    {
        public static async Task<int> Main(string[] args)
        {
            Console.Title = "Sen Server";
            try
            {
                var host = new HostBuilder()
                    .UseEnvironment(Environments.Development)
                    .UseOrleans((context, siloBuilder) =>
                    {
                        var isDevelopment = context.HostingEnvironment.IsDevelopment();
                        siloBuilder
                            .Configure<ConnectionOptions>(options =>
                            {
                                options.ProtocolVersion = Orleans.Runtime.Messaging.NetworkProtocolVersion.Version2;
                            })
                            .AddSimpleMessageStreamProvider("SMSProvider");

                        if (isDevelopment)
                        {
                            siloBuilder.UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev");
                        }
                        else
                        {
                        }
                    })
                    .ConfigureLogging(logging => logging.AddNLog())
                    .Build();

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
