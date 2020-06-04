using Orleans;
using Orleans.Concurrency;
using Sen.DataModel;
using Sen.Interfaces;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Sen.Grains
{
    public class ProxyConnectionGrain<TUnionData, TGrainState> : Grain<TGrainState>, IProxyConnection
        where TUnionData : IUnionData
    {
        public IPEndPoint LocalAddress { get; private set; }

        public IPEndPoint RemoteAddress { get; private set; }

        public virtual ValueTask InitConnection(EndPoint local, EndPoint remote)
        {
            Console.WriteLine($"Init connection {local}, {remote}");
            LocalAddress = local as IPEndPoint;
            RemoteAddress = remote as IPEndPoint;

            return default;
        }

        public Task<Immutable<byte[]>> Read(Immutable<byte[]> data)
        {
            WiredData<TUnionData> message = MessagePack.MessagePackSerializer.Deserialize<WiredData<TUnionData>>(data.Value);

            return Task.FromResult(default(Immutable<byte[]>));
        }

        public ValueTask Disconnect()
        {
            Console.WriteLine("Disconnect " + RemoteAddress.ToString());
            return default;
        }
    }
}
