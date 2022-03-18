using Demo.Interfaces;
using Demo.Interfaces.Message;
using Sen;
using System.Threading.Tasks;

namespace Demo.Grains
{
    public class GameWorld : AbstractLobby<LobbyState>, IGameWorld
    {
        public const string GameWorldName = "GameWorld";

        public ValueTask<JoinRoom> HandleMessage(JoinRoom joinRoomMessage, IPlayer sender, NetworkOptions networkOptions)
        {
            if (joinRoomMessage.RoomName == GameWorldName)
            {
                return new ValueTask<JoinRoom>(joinRoomMessage);
            }
            return new ValueTask<JoinRoom>(joinRoomMessage);
        }
    }
}
