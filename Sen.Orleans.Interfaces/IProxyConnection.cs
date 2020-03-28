using System;
using System.Threading.Tasks;

namespace Sen.OrleansInterfaces
{
    public interface IProxyConnection : Orleans.IGrainWithGuidKey
    {
        Task<string> Test(string message);
        Task<bool> InitConnection();
        Task Subscribe(IDataObserver observer);
        Task UnSubscribe(IDataObserver observer);
    }
}
