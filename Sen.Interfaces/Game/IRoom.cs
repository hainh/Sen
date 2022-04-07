
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sen
{
    public interface IRoom : Orleans.IGrainWithStringKey
    {
        ValueTask<long> GetMatchId();
        ValueTask<string> GetRoomName();
        ValueTask<string?> GetPassword();
        ValueTask<int> GetPlayerLimit();
        ValueTask<bool> IsFull();

        ValueTask<ILobby?> GetParent();
        ValueTask<bool> IsLobby();

        ValueTask<ICollection<IPlayer>> GetPlayers();

        ValueTask SetParent(ILobby? room);

        ValueTask<bool> JoinRoom(IPlayer player, string playerName);
        ValueTask<bool> LeaveRoom(IPlayer player, string playerName);

        ValueTask<IUnionData> HandleRoomMessage(IUnionData message, IPlayer sender, NetworkOptions networkOptions);
    }
}
