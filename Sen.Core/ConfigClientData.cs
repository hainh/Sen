using Senla.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.SocketServer
{
    internal class ConfigClientData : IDataContainer
    {
        public ConfigClientData(byte code, Dictionary<byte, object> paramters)
        {
            Code = code;
            Parameters = paramters;
        }

        public byte Code { get; set; }

        public Dictionary<byte, object> Parameters { get; set; }

        public byte ServiceCode
        {
            get
            {
                return (byte)ServiceType.ConfigData;
            }
        }
    }
}
