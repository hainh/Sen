using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.Configuration
{
    public class Settings
        : ConfigurationElement
    {
        [ConfigurationProperty("MaximunMessageSize", DefaultValue = 51200)]
        [IntegerValidator(ExcludeRange = false, MinValue = 10, MaxValue = 100000)]
        public int MaximunMessageSize
        {
            get
            {
                return (int)this["MaximunMessageSize"];
            }
            set
            {
                this["MaximunMessageSize"] = value;
            }
        }

        [ConfigurationProperty("TcpMaxIdleTime", DefaultValue = 6000, IsRequired = true)]
        [IntegerValidator(ExcludeRange = false, MaxValue = int.MaxValue, MinValue = 1)]
        public int TcpMaxIdleTime
        {
            get
            {
                return (int)this["TcpMaxIdleTime"];
            }
            set
            {
                this["TcpMaxIdleTime"] = value;
            }
        }

        [ConfigurationProperty("TcpMinIdleTime", DefaultValue = 5000, IsRequired = true)]
        [IntegerValidator(ExcludeRange = false, MaxValue = int.MaxValue, MinValue = 1)]
        public int TcpMinIdleTime
        {
            get
            {
                return (int)this["TcpMinIdleTime"];
            }
            set
            {
                this["TcpMinIdleTime"] = value;
            }
        }

        [ConfigurationProperty("TcpEndpoint", IsDefaultCollection = false, IsRequired = true)]
        public TcpEndpoint TcpEndpoint
        {
            get
            {
                TcpEndpoint tcpEndpoint = (TcpEndpoint)base["TcpEndpoint"];
                return tcpEndpoint;
            }
        }

        [ConfigurationProperty("UdpEndpoint", IsDefaultCollection = false, IsRequired = true)]
        public UdpEndpoint UdpEndpoint
        {
            get
            {
                UdpEndpoint udpEndpoint = (UdpEndpoint)base["UdpEndpoint"];
                return udpEndpoint;
            }
        }

        [ConfigurationProperty("WebSocketEndpoint", IsDefaultCollection = false, IsRequired = true)]
        public WebSocketEndpoint WebSocketEndpoint
        {
            get
            {
                WebSocketEndpoint webSocketEndpoint = (WebSocketEndpoint)base["WebSocketEndpoint"];
                return webSocketEndpoint;
            }
        }
    }
}
