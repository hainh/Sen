using Akka.Actor;
using Senla.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Gamer.Internal
{
    internal class InternalPeer : PeerBase
    {
        private IActorRef _peerActor;

        public InternalPeer(ISocketPeer socketPeer)
            : base(socketPeer)
        {

        }

        internal void SetPeerActor(IActorRef peerActor)
        {
            _peerActor = peerActor;
        }

        public override void OnDisconnect(DisconnectReason reason)
        {
            _peerActor.Tell(new OnDisconnect(reason));
        }

        public override void OnOperationRequest(OperationData operationData, SendParameters sendParameters)
        {
            _peerActor.Tell(new OperationRequest(operationData, sendParameters));
        }
    }

    internal class OperationRequest
    {
        internal OperationRequest(OperationData operationData, SendParameters sendParameters)
        {
            OperationData = operationData;
            SendParameters = sendParameters;
        }

        public OperationData OperationData { get; private set; }

        public SendParameters SendParameters { get; private set; }
    }

    internal class OperationResponse
    {
        internal OperationResponse(OperationData operationData, SendParameters sendParameters)
        {
            OperationData = operationData;
            SendParameters = sendParameters;
        }

        public OperationData OperationData { get; private set; }

        public SendParameters SendParameters { get; private set; }
    }

    internal class Event
    {
        internal Event(EventData eventData, SendParameters sendParameters)
        {
            EventData = eventData;
            SendParameters = sendParameters;
        }

        public EventData EventData { get; private set; }

        public SendParameters SendParameters { get; private set; }
    }

    internal class OnDisconnect
    {
        internal OnDisconnect(DisconnectReason reason)
        {
            Reason = reason;
        }

        public DisconnectReason Reason { get; private set; }
    }
}
