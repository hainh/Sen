using Orleans;
using Sen.OrleansInterfaces;
using System;
using System.Threading.Tasks;

namespace Sen.OrleansGrains
{
    public class ProxyConnectionGrain : Grain, IProxyConnection
    {
        public Task<bool> InitConnection()
        {
            return Task.FromResult(false);
        }

        public Task<string> Test(string message)
        {
            Console.WriteLine($"{GetType().FullName} message received: greeting = '{message}'");

            return Task.FromResult($"You said: '{message}', I say: Fuckoff!");
        }
    }
}
