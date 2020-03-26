using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core.Heartbeat
{
    public class Ping : IDataContainer
    {
        static readonly long ServerStartTime = DateTime.UtcNow.Ticks;


        public Ping() { }

        public Ping(byte code, Dictionary<byte, object> parameters)
        {
            Code = code;
            Parameters = parameters;
        }

        public byte Code { get; set; }

        public Dictionary<byte, object> Parameters { get; set; }

        public byte ServiceCode
        {
            get
            {
                return (byte)ServiceType.PingData;
            }
        }

        public Ping SetPingTime()
        {
            var lastPingTick = DateTime.UtcNow.Ticks - ServerStartTime;
            lastPingTick /= TimeSpan.TicksPerMillisecond;

            Parameters = new Dictionary<byte, object> { { 0, (ulong)lastPingTick } };
            return this;
        }

        public int GetPingTime()
        {
            int pingTime = (int)((DateTime.UtcNow.Ticks - ServerStartTime) / TimeSpan.TicksPerMillisecond - (long)(ulong)Parameters[0]);
            return pingTime;
        }
    }
}
