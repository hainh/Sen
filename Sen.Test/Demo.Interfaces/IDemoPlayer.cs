using Orleans;
using Sen.Game;
using System;

namespace Demo.Interfaces
{
    public interface IDemoPlayer : IPlayer, IGrainWithStringKey
    {
    }
}
