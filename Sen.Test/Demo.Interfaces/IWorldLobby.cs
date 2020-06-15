using Orleans;
using Sen.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Interfaces
{
    public interface IWorldLobby : ILobby, IGrainWithStringKey
    {
    }
}
