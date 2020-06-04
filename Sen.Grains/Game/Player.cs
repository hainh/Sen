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
        private delegate ValueTask<TUnionData> ProcessMessage(TUnionData message);
        private readonly Dictionary<RuntimeTypeHandle, ProcessMessage>
            _messageHandlers = new Dictionary<RuntimeTypeHandle, ProcessMessage>();

        protected IRoom _room;
        protected bool _isBot;

        public IPEndPoint LocalAddress { get; private set; }
        public IPEndPoint RemoteAddress { get; private set; }
        public ValueTask<IRoom> Room => new ValueTask<IRoom>(_room);

        private void InitializeMessageHandler()
        {
            Type type = GetType();
            MethodInfo[] allMethods = type.GetMethods().Where(isProcessMothod).ToArray();
            foreach (var method in allMethods)
            {
                ProcessMessage del = method.Attributes == MethodAttributes.Static
                    ? (ProcessMessage)method.CreateDelegate(typeof(ProcessMessage))
                    : (ProcessMessage)method.CreateDelegate(typeof(ProcessMessage), this);
                _messageHandlers.Add(method.GetParameters()[0].ParameterType.TypeHandle, del);
            }

            static bool isProcessMothod(MethodInfo method)
            {
                if (method.Name != "Process")
                {
                    return false;
                }
                ParameterInfo[] @params = method.GetParameters();
                static bool isMessagePackOject(CustomAttributeData attr)
                    => attr.AttributeType == typeof(MessagePackObjectAttribute);
                if (@params.Length != 1 || !@params[0].ParameterType.CustomAttributes.Any(isMessagePackOject))
                {
                    return false;
                }
                return true;
            }
        }

        public ValueTask SetRoom(IRoom room)
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

            Console.WriteLine($"Init connection {local}, {remote}");
            LocalAddress = local as IPEndPoint;
            RemoteAddress = remote as IPEndPoint;
            InitializeMessageHandler();

            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// Process a message data. Inherited class create its own overloaded version to
        /// process a specific message.
        /// </summary>
        /// <returns>A <see cref="IUnionData"/> will be serialized and returned to game client or null to send nothing</returns>
#pragma warning disable IDE0052 // Remove unread private members
        private TUnionData Process(TUnionData _) => null;
#pragma warning restore IDE0052 // Remove unread private members

        /// <summary>
        /// Read data from client. Calling <see cref="Process"/> overloaded mothods in this class
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<Immutable<byte[]>> Read(Immutable<byte[]> data)
        {
            WiredData<TUnionData> wiredData = MessagePackSerializer.Deserialize<WiredData<TUnionData>>(data.Value);
            TUnionData returnedData = null;
            if (_messageHandlers.TryGetValue(Type.GetTypeHandle(wiredData.Data), out ProcessMessage processMessage))
            {
                returnedData = await processMessage(wiredData.Data);
            }
            else if (_room != null)
            {
                returnedData = (TUnionData)await _room.HandleMessage(wiredData.Data, this);
            }

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
