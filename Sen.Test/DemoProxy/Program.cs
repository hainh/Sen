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
                .RunOrleansProxyClient(new SenProxy(), new Commander());
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
