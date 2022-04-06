using Orleans;
using System.Threading.Tasks;

namespace Sen
{
    public interface IServerToServerGrain : IProxyConnection
    {
        ValueTask<bool> InitConnection(string leafServerName, IClientObserver observer);
    }
}
