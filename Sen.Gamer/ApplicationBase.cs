using System;
using Akka.Actor;
using Senla.Core;
using Senla.Gamer.Internal;

namespace Senla.Gamer
{
    /// <summary>
    /// Base class for "Senla.Gamer" application that use Akka.Net
    /// </summary>
    public abstract class ApplicationBase : IApplication
    {
        public const string SystemName = "Senla.Gamer";

        /// <summary>
        /// ActorSystem for all applications that employ Senla.Gamer
        /// </summary>
        public static ActorSystem ActorSystem { get; set; }

        /// <summary>
        /// Ref to <see cref="Internal.PeersManager"/> actor
        /// </summary>
        public IActorRef PeersManager { get { return _peersManager; } }
        private IActorRef _peersManager;

        /// <summary>
        /// Don't call this constructor from your code
        /// </summary>
        public ApplicationBase()
        {
            if (ActorSystem == null)
            {
                ActorSystem = ActorSystem.Create(SystemName);
            }

            Instance = this;
        }

        /// <summary>
        /// Name of this application as in configuration
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Instance of <see cref="ApplicationBase"/>
        /// </summary>
        public static ApplicationBase Instance { get; private set; }

        /// <summary>
        /// Called as a connection is attemped to make. Overriden.
        /// </summary>
        /// <param name="socketPeer"></param>
        /// <returns></returns>
        public PeerBase CreatePeer(ISocketPeer socketPeer)
        {
            var peer = new InternalPeer(socketPeer);

            _peersManager.Tell(new PeersManager.CreateNewPeer(peer));

            return peer;
        }

        /// <summary>
        /// Create a receipt used to instantiate a new  <see cref="PeerActor"/>.
        /// Derived class can override to create custom <see cref="PeerActor"/>.
        /// </summary>
        /// <param name="socketPeer">A socket peer of type <see cref="PeerBase"/> to create a new <see cref="PeerActor"/></param>
        /// <returns>A <see cref="Func{PeerActor}"/> that used to instantiate new <see cref="PeerActor"/></returns>
        public virtual System.Linq.Expressions.Expression<Func<PeerActor>> CreatePeerActorReceipt(PeerBase socketPeer)
        {
            return () => new PeerActor(socketPeer);
        }

        /// <summary>
        /// Called on application set up
        /// </summary>
        public void Setup()
        {
            _peersManager = ActorSystem.ActorOf(Props.Create(() => new PeersManager(this)),
                "PeersManager of " + (AppName?.ToString() ?? GetType().Name));

            OnAppStart();
        }

        /// <summary>
        /// Called on app started
        /// </summary>
        public abstract void OnAppStart();

        /// <summary>
        /// Called on app shut down
        /// </summary>
        public abstract void Shutdown();

        internal void PlayerDisconnected(Player player, bool v)
        {
            throw new NotImplementedException();
        }
    }

}
