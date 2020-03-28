using System;
using System.Net;
using System.Threading.Tasks;

namespace Sen.OrleansInterfaces
{
    public interface IProxyConnection : Orleans.IGrainWithGuidKey
    {
        Task InitConnection(EndPoint local, EndPoint remote);
        Task<byte[]> Read(byte[] data); // Read data from client
        Task Disconnect();
    }
}
