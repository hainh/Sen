﻿using Orleans;
using Orleans.Concurrency;
using Sen;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public interface IRoom
    {
        ValueTask<long> GetMatchId();
        ValueTask<string> GetRoomName();
        ValueTask<string> GetPassword();
        ValueTask<int> GetPlayerLimit();
        ValueTask<bool> IsFull();

        ValueTask<ILobby> GetParent();
        ValueTask<bool> IsLobby();

        ValueTask<ICollection<IPlayer>> GetPlayers();

        ValueTask SetParent(ILobby room);

        ValueTask<bool> JoinRoom(IPlayer player, string playerName);

        ValueTask<IUnionData> HandleRoomMessage(IUnionData message, IPlayer player);
    }
}
