using Sen.Client.Unity.Abstract;

namespace Sen.Client.Unity
{
    public interface IMessageHandler
    {
        void HandleMessage(IUnionData message, NetworkOptions networkOptions);
        void OnStateChange(ConnectionState state);
    }
}
