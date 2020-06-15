using Demo.Interfaces;
using Sen.Proxy;
using Sen.Utilities.Console;
using System.Threading.Tasks;

namespace DemoProxy
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await new OrleansProxyClient<IDemoPlayer>()
                .RunOrleansProxyClient(new DotNettyProxy(), new SimpleCommander());
        }
    }
}
