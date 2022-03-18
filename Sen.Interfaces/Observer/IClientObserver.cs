using Orleans;

namespace Sen
{
    public interface IClientObserver : IGrainObserver
    {
        void ReceiveData(Orleans.Concurrency.Immutable<byte[]> data);
    }
}
