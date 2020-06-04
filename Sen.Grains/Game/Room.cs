using MessagePack;
using Orleans;
using Sen.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Game
{
    public class Room : Grain, IRoom
    {
        private delegate ValueTask<IUnionData> ProcessMessage(IUnionData message, IPlayer player);
        private readonly Dictionary<RuntimeTypeHandle, ProcessMessage>
            _messageHandlers = new Dictionary<RuntimeTypeHandle, ProcessMessage>();

        protected object _matchId;
        protected string _roomName;
        protected string _password;
        protected int _playerLimit;
        protected ILobby _parent;
        protected List<IPlayer> _players;

        public ValueTask<object> MatchId => new ValueTask<object>(_matchId);

        public ValueTask<string> RoomName => new ValueTask<string>(_roomName);

        public ValueTask<string> Password => new ValueTask<string>(_password);

        public ValueTask<int> PlayerLimit => new ValueTask<int>(_playerLimit);

        public ValueTask<ILobby> Parent => new ValueTask<ILobby>(_parent);

        public ValueTask<bool> IsLobby => new ValueTask<bool>(false);

        public ValueTask<List<IPlayer>> Players => new ValueTask<List<IPlayer>>(_players);

        public Room()
        {
            InitializeMessageHandler();
        }

        /// <summary>
        /// Process a message data. Inherited class create its own overloaded version to
        /// process a specific message.
        /// </summary>
        /// <returns>A <see cref="IUnionData"/> will be serialized and returned to game client or null to send nothing</returns>
#pragma warning disable IDE0052 // Remove unread private members
        private IUnionData Process(IUnionData _) => null;
#pragma warning restore IDE0052 // Remove unread private members

        /// <summary>
        /// Handle a message from client. Calling <see cref="Process"/> overloaded mothods in this class as a handler
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async ValueTask<IUnionData> HandleMessage(IUnionData message, IPlayer player)
        {
            IUnionData returnedData = null;
            if (_messageHandlers.TryGetValue(Type.GetTypeHandle(message), out ProcessMessage processMessage))
            {
                returnedData = await processMessage(message, player);
            }

            return returnedData;
        }

        public ValueTask JoinRoom(IPlayer player)
        {
            throw new NotImplementedException();
        }

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
                static bool isIPlayerType(Type type)
                {
                    return (type.IsInterface && type.Equals(typeof(IPlayer)))
                        || type.GetInterface(nameof(IPlayer)) != null;
                }
                if (@params.Length != 2
                    || !@params[0].ParameterType.CustomAttributes.Any(isMessagePackOject)
                    || !isIPlayerType(@params[1].ParameterType))
                {
                    return false;
                }
                return true;
            }
        }
    }
}
