using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Senla.Core
{
    public interface IApplication
    {
        string AppName { get; set; }

        PeerBase CreatePeer(ISocketPeer socketPeer);

        void Setup();

        void Shutdown();
    }
}
