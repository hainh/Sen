using Orleans;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sen.OrleansInterfaces
{
    public interface IDataObserver : IGrainObserver
    {
        void ReceiveData(byte[] data);
    }
}
