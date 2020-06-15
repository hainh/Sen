using Orleans.Concurrency;
using Orleans.Streams;
using Sen.Game;
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
        IAsyncStream<Immutable<byte[]>> CreateStream(IPlayer player);
    }
}
