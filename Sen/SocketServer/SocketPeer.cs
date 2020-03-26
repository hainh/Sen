using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Senla.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer
{
    class SocketPeer : ISocketPeer
    {
        private IChannelHandlerContext _context;

        public SocketPeer(IChannelHandlerContext context)
        {
            _context = context;
        }

        public int ConnectionId
        {
            get
            {
                return _context.Channel.Id.AsLongText().GetHashCode();
            }
        }

        public string LocalIp
        {
            get
            {
                return (_context.Channel.LocalAddress as IPEndPoint)?.Address.ToString();
            }
        }

        public int LocalPort
        {
            get
            {
                return (_context.Channel.LocalAddress as IPEndPoint)?.Port ?? 0;
            }
        }

        public string RemoteIp
        {
            get
            {
                return (_context.Channel.RemoteAddress as IPEndPoint)?.Address.ToString();
            }
        }

        public int RemotePort
        {
            get
            {
                return (_context.Channel.RemoteAddress as IPEndPoint)?.Port ?? 0;
            }
        }

        public void DisconnectAsync()
        {
            var context = _context;
            _context.DisconnectAsync().ContinueWith(t => context.CloseAsync());
            _context = null;
        }

        public void Flush()
        {
            _context.Flush();
        }

        public void WriteAndFlushAsync(object message)
        {
            _context.Channel.WriteAndFlushAsync(message);
        }

        public void WriteAsync(object message)
        {
            _context.Channel.WriteAsync(message);
        }
    }
}
