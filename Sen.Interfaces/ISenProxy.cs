using Microsoft.Extensions.Hosting;
using Orleans.Concurrency;
using Orleans.Streams;
using Sen.Game;

namespace Sen.Interfaces
{
    public interface ISenProxy : IHost
    {
        void SetGrainFactory(IPlayerFactory grainFactory);
    }

    public interface IPlayerFactory
    {
        IPlayer CreatePlayer(string playerId);
        IAsyncStream<Immutable<byte[]>> CreateStream(IPlayer player);
    }
}
