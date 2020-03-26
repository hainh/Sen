using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public enum DisconnectReason
    {
        ConnectionLost,
        DataDeserializeFailed,
        ServerDisconnect,
        ConnectionTimeout,
    }
}
