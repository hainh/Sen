using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Sen.Proxy
{
    public interface ISenProxy
    {
        void SetGrainFactory(IPlayerFactory grainFactory);
        Task StartAsync();
        Task StopAsync();
    }

    public interface IPlayerFactory
    {
        IPlayer CreatePlayer(string playerId);
        Task<IClientObserver> CreateObserver<T>(T player) where T : class, IClientObserver;
    }
}
