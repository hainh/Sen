using Orleans;
using Orleans.Concurrency;
using Sen.DataModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Game
{
    public interface IRoom
    {
        ValueTask<long> MatchId { get; }
        ValueTask<string> RoomName { get; }
        ValueTask<string> Password { get; }
        ValueTask<int> PlayerLimit { get; }
        ValueTask<bool> IsFull { get; }

        ValueTask<ILobby> Parent { get; }
        ValueTask<bool> IsLobby { get; }

        ValueTask<ICollection<IPlayer>> Players { get; }

        ValueTask SetParent(ILobby room);

        ValueTask<bool> JoinRoom(IPlayer player);

        ValueTask<IUnionData> HandleRoomMessage(IUnionData message, IPlayer player);
    }
}
