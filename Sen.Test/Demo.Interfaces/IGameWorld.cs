﻿using Orleans;
using Sen;

namespace Demo.Interfaces
{
    public interface IGameWorld : ILobby, IGrainWithStringKey
    {
    }
}
