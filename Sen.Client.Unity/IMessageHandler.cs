
namespace Sen
{
    public interface IMessageHandler
    {
        void HandleMessage(IUnionData message, NetworkOptions networkOptions);
        void OnStateChange(ConnectionState state);
    }
}
