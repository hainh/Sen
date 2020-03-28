using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Sen.Server
{
    class SenServer
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var host = new HostBuilder()
                    .UseEnvironment(Environments.Development)
                    .UseOrleans((context, siloBuilder) =>
                    {
                        var isDevelopment = context.HostingEnvironment.IsDevelopment();
                        siloBuilder
                            .UseLocalhostClustering(serviceId: "SenProxyApp", clusterId: "dev")
                            .Configure<ConnectionOptions>(options =>
                            {
                                options.ProtocolVersion = Orleans.Runtime.Messaging.NetworkProtocolVersion.Version2;
                            });
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();
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
