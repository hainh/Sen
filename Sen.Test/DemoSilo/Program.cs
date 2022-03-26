using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Sen.Server;
using System.Threading.Tasks;

namespace DemoSilo
{
    class Program
    {
        public static async Task<int> Main()
        {
            return await SenServer.Run((Microsoft.Extensions.Hosting.HostBuilderContext context, ISiloBuilder siloBuilder) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                siloBuilder
                    .Configure<ConnectionOptions>(options =>
                    {
                        options.ProtocolVersion = Orleans.Runtime.Messaging.NetworkProtocolVersion.Version2;
                    })
                    .AddSimpleMessageStreamProvider("SMSProvider")
                    .AddMemoryGrainStorageAsDefault();

                if (isDevelopment)
                {
                    siloBuilder.UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev");
                }
                else
                {
                    siloBuilder.UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev");
                }
            });
        }
    }
}
