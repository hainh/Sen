
namespace Sen
{
    public interface IMessageHandler
    {
        void DispatchMessage(IUnionData message, NetworkOptions networkOptions);
        void OnStateChange(ConnectionState state);
    }
}
