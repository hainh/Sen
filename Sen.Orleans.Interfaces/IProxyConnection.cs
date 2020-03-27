using System;
using System.Threading.Tasks;

namespace Sen.OrleansInterfaces
{
    public interface IProxyConnection : Orleans.IGrainWithIntegerKey
    {
        Task<string> Test(string message);
        Task<bool> InitConnection();
    }
}
