using DotNetty.Buffers;
using Senla.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer
{
    public class ReceivedDataWrapper
    {
        public SendParameters SendParameters;

        public IByteBuffer Buffer;

        public ReceivedDataWrapper(IByteBuffer buffer, SendParameters sendParameters)
        {
            Buffer = buffer;
            SendParameters = sendParameters;
        }
    }
}
