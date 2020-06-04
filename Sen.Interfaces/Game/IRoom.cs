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
        ValueTask<object> MatchId { get; }
        ValueTask<string> RoomName { get; }
        ValueTask<string> Password { get; }
        ValueTask<int> PlayerLimit { get; }

        ValueTask<ILobby> Parent { get; }
        ValueTask<bool> IsLobby { get; }

        ValueTask<List<IPlayer>> Players { get; }

        ValueTask JoinRoom(IPlayer player);

        ValueTask<IUnionData> HandleMessage(IUnionData message, IPlayer player);
    }
}
