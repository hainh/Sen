using Orleans;
using System.Threading.Tasks;

namespace Sen
{
    public interface IServerToServerGrain : IProxyConnection
    {
        ValueTask InitConnection(string leafServerName, IClientObserver observer);
    }
}
