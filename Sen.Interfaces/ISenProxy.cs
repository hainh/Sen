using Microsoft.Extensions.Hosting;
using Sen.Game;

namespace Sen.Interfaces
{
    public delegate IPlayer GrainFactory(string playerId);

    public interface ISenProxy : IHost
    {
        void SetGrainFactory(GrainFactory grainFactory);
    }
}
