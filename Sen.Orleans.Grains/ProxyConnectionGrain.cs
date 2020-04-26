using Orleans;
using Orleans.Concurrency;
using Sen.OrleansInterfaces;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Sen.OrleansGrains
{
    public class ProxyConnectionGrain : Grain, IProxyConnection
    {
        public IPEndPoint LocalAddress { get; private set; }

        public IPEndPoint RemoteAddress { get; private set; }

        public Task InitConnection(EndPoint local, EndPoint remote)
        {
            Console.WriteLine($"Init connection {local}, {remote}");
            LocalAddress = local as IPEndPoint;
            RemoteAddress = remote as IPEndPoint;

            // TODO: init player...

            return Task.CompletedTask;
        }

        public Task<Immutable<byte[]>> Read(Immutable<byte[]> data)
        {
            return Task.FromResult(data);
        }

        public Task<byte[]> Read(byte[] data)
        {
            return Task.FromResult(data);
        }

        public Task Disconnect()
        {
            Console.WriteLine("Disconnect " + RemoteAddress.ToString());
            return Task.CompletedTask;
        }
    }
}
