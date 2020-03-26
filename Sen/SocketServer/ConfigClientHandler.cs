using DotNetty.Transport.Channels;
using Senla.Core;
using Senla.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer
{
    /// <summary>
    /// This handler is for sending config to client then is removed after sending
    /// </summary>
    public class ConfigClientHandler : ChannelHandlerAdapter
    {
        public ConfigClientHandler(Settings settings)
        {
            AppSettings = settings;
        }

        public Settings AppSettings { get; private set; }

        public override void HandlerAdded(IChannelHandlerContext context)
        {
            var config = new ConfigClientData(0, new Dictionary<byte, object>
            {
                { 0, (uint)(AppSettings.TcpMinIdleTime / 1000) },
            });

            var reliableHandler = context.Channel.Pipeline.Get<ReliableServerHandler>();

            // send config
            reliableHandler._peer.sendData(config, new SendParameters());
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            var task = context.WriteAsync(message);
            // remove after sending config
            context.Channel.Pipeline.Remove(this);
            return task;
        }
    }
}
