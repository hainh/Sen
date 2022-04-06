using Demo.Interfaces;
using Orleans;
using Sen.Proxy;
using Sen.Utilities.Console;
using System;
using System.Threading.Tasks;

namespace DemoProxy
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var client = new OrleansProxyClient<IDemoPlayer, IDemoServerToServer>();
            _ = Task.Run(async () =>
            {
                while (!client.Connected)
                {
                    await Task.Delay(200);
                }

                await Task.Delay(1000);

                var player = client.OrleansClusterClient.GetGrain<IDemoPlayer>("demo");

                Console.WriteLine(await player.IsBot());
            });
            return await client.RunOrleansProxyClient(new SenProxy(), builder => builder.UseLocalhostClustering(serviceId: "SenServer", clusterId: "dev"), new Commander());
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    class Commander : SimpleCommander
    {
        static void Ha(string hoi)
        {
            Console.WriteLine("?" + hoi);
        }

        static void He([ParameterHelper("what is kee?")]string keee)
        {
            Console.WriteLine("?" + keee);
        }

        [CommandHelper("kakakak")]
        [ParameterHelper("print out")]
        static void Ka(string kaka)
        {
            Console.Write("?");
            Console.Write(kaka);
        }
    }
#pragma warning restore IDE0051 // Remove unused private members
}
