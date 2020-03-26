using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Gamer.Message
{
    public class LeaveRoom
    {
        public LeaveRoom(Player player, bool isDisconnected)
        {
            Player = player;
            IsDisconnected = isDisconnected;
        }

        public Player Player { get; private set; }

        public bool IsDisconnected { get; private set; }
    }
}
