using System.Threading.Tasks;

namespace Sen.Proxy
{
    public interface ISenProxy
    {
        void SetGrainFactory(IProxyServiceProvider grainFactory);
        Task StartAsync();
        Task StopAsync();
    }

    public interface IProxyServiceProvider
    {
        IPlayer GetPlayer(string playerId);

        IServerToServerGrain CreateServerToServerPeer(string leafServerId);

        IAuthService GetAuthServiceGrain();

        Task<IClientObserver> CreateObserver<T>(T player) where T : class, IClientObserver;
    }
}
