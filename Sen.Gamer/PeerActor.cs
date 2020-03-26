using Senla.Core;
using Senla.Gamer.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Gamer
{
    public class PeerActor : Akka.Actor.ReceiveActor
    {
        private PeerBase _peer;

        private Player _player;

        public PeerActor(PeerBase rawPeer)
        {
            _peer = rawPeer;

            Receive(new Action<ReplacePlayer>(replace));
        }

        private void replace(ReplacePlayer replace)
        {
            this._player = replace.Player;
        }

        #region
        public class ReplacePlayer
        {
            public ReplacePlayer(Player player)
            {
                Player = player;
            }

            public Player Player { get; private set; }
        }
        #endregion
    }
}
