using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.Configuration
{
    public class WebSocketEndpoint
        : ConfigurationElement
    {
        [ConfigurationProperty("Port", DefaultValue = 807)]
        [IntegerValidator(ExcludeRange = false, MinValue = 600, MaxValue = 65535)]
        public int Port
        {
            get
            {
                return (int)this["Port"];
            }
            set
            {
                this["Port"] = value;
            }
        }

        [ConfigurationProperty("Address", DefaultValue = "0.0.0.0")]
        [RegexStringValidator("^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
        protected string Address
        {
            get
            {
                return (string)this["Address"];
            }
            set
            {
                this["Address"] = value;
            }
        }

        public EndPoint Endpoint
        {
            get
            {
                return new IPEndPoint(IPAddress.Parse(Address), Port);
            }
        }
    }
}
