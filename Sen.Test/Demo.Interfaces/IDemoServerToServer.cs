using Orleans;
using Sen;

namespace Demo.Interfaces
{
    public interface IDemoServerToServer : IServerToServerGrain, IGrainWithStringKey
    {
    }
}
