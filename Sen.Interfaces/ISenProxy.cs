using Microsoft.Extensions.Hosting;
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
    }
}
