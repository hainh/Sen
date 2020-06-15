using Demo.Interfaces;
using Demo.Interfaces.Message;
using Sen.Game;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class WorldLobby : Lobby, IWorldLobby
    {
        public ValueTask<IDemoUnionData> HandleMessage(JoinRoom joinRoomMessage)
        {
            return new ValueTask<IDemoUnionData>(joinRoomMessage);
        }
    }
}
