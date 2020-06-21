using Demo.Interfaces;
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
            return await new OrleansProxyClient<IDemoPlayer>()
                .RunOrleansProxyClient(new DotNettyProxy(), new Commander());
        }
    }

    class Commander : SimpleCommander
    {
        void Ha(string hoi)
        {
            Console.WriteLine("hah?");
        }

        public void He([ParameterHelper("what is kee?")]string keee)
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
}
