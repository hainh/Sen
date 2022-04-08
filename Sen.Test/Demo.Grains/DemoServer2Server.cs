using Demo.Interfaces;
using Orleans.Concurrency;
using Sen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class DemoServer2Server : Orleans.Grain, IDemoServerToServer, Orleans.IGrainWithStringKey
    {
        public ValueTask InitConnection(string leafServerName, IClientObserver observer)
        {
            throw new NotImplementedException();
        }

        public ValueTask OnDisconnect()
        {
            throw new NotImplementedException();
        }

        public ValueTask<Immutable<byte[]>> OnReceivedData(Immutable<byte[]> data)
        {
            throw new NotImplementedException();
        }
    }
}
