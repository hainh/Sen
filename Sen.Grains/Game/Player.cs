using MessagePack;
using Orleans;
using Orleans.Concurrency;
using Sen.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Game
{
    public abstract class Player<TUnionData, TGrainState> : Grain<TGrainState>, IPlayer
         where TUnionData : class, IUnionData
    {
        protected IRoom _room;
        protected bool _isBot;

        public IPEndPoint LocalAddress { get; private set; }
        public IPEndPoint RemoteAddress { get; private set; }
        public ValueTask<IRoom> Room => new ValueTask<IRoom>(_room);

        public abstract ValueTask<string> Name { get; }

        public ValueTask SetRoomJoined(IRoom room)
        {
            _room = room;
            return default;
        }

        public ValueTask<bool> IsBot => new ValueTask<bool>(_isBot);

        public virtual ValueTask<bool> InitConnection(EndPoint local, EndPoint remote, string accessToken)
        {
            if (LocalAddress != null)
            {
                return new ValueTask<bool>(true);
            }

            LocalAddress = local as IPEndPoint;
            RemoteAddress = remote as IPEndPoint;

            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// Handle a message object. Inherited class create its own overloaded version to
        /// handle a specific message.
        /// </summary>
        /// <returns>A <see cref="IUnionData"/> will be serialized and returned to game client or null to send nothing</returns>
        protected async ValueTask<TUnionData> HandleMessage(TUnionData message)
        {
            if (_room != null)
            {
                return (TUnionData)await _room.HandleRoomMessage(message, this);
            }
            return null;
        }

        /// <summary>
        /// Read data from client. Calling <see cref="HandleMessage"/> overloaded mothods in this class.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<Immutable<byte[]>> Read(Immutable<byte[]> data)
        {
            WiredData<TUnionData> wiredData = MessagePackSerializer.Deserialize<WiredData<TUnionData>>(data.Value);
            dynamic message = wiredData.Data;
            TUnionData returnedData = await ((dynamic)this).HandleMessage(message);
            if (returnedData != null)
            {
                wiredData.Data = returnedData;
                return MessagePackSerializer.Serialize(wiredData).AsImmutable();
            }
            return new Immutable<byte[]>(null);
        }

        public ValueTask Write(Immutable<IUnionData> data, WiredDataType underlieData = WiredDataType.Normal)
        {
            throw new NotImplementedException();
        }

        public ValueTask Disconnect()
        {
            Console.WriteLine("Disconnect " + RemoteAddress.ToString());
            return default;
        }

        public abstract ValueTask OnDisconnect();
    }
}
