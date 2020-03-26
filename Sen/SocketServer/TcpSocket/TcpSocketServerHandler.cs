using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Senla.Core;
using Senla.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer.TcpSocket
{
    public class TcpSocketServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var messageWrapper = new ReceivedDataWrapper((IByteBuffer)message, new SendParameters());
            context.FireChannelRead(messageWrapper);
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            var sendingDataWrapper = message as Core.SendingDataWrapper;
            var data = sendingDataWrapper.Data as DequeBuffer<byte>;
            data.Heapyfy();

            var buffer = context.Allocator.Buffer(data.Count);
            buffer.WriteBytes(data.UnderlyArray, 0, data.Count);
            return context.WriteAsync(buffer);
        }
    }
}
