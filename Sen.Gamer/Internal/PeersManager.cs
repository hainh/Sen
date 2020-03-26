using Akka.Actor;
using System;

namespace Senla.Gamer.Internal
{
    /// <summary>
    /// 
    /// </summary>
    internal class PeersManager : ReceiveActor
    {
        #region Messages
        /// <summary>
        /// Create a new PeerActor that act on behalf of the phisical <see cref="PeerBase"/>
        /// </summary>
        internal class CreateNewPeer
        {
            public CreateNewPeer(InternalPeer peer)
            {
                Peer = peer;
            }

            public InternalPeer Peer { get; private set; }
        }

        /// <summary>
        /// Message as the peer is disconnected
        /// </summary>
        internal class PeerDisconnected
        {
            public PeerDisconnected(InternalPeer peer)
            {
                this.Peer = peer;
            }

            public InternalPeer Peer { get; private set; }
        }
        #endregion

        private ApplicationBase application;

        public PeersManager(ApplicationBase app)
        {
            this.application = app;

            Receive(new Action<CreateNewPeer>(createPeer));
        }

        void createPeer(CreateNewPeer create)
        {
            var peerActor = Context.ActorOf(Props.Create(this.application.CreatePeerActorReceipt(create.Peer)),
                    create.Peer.RemoteIp + ":" + create.Peer.RemotePort);
            create.Peer.SetPeerActor(peerActor);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(-1, 1000, e =>
            {
                return Directive.Resume;
            });
        }
    }
}
