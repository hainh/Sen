using Senla.Core.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public class EventData : AbstractDataContainer
    {
        public EventData(byte eventCode, Dictionary<byte, object> parameters)
        {
            Code = eventCode;
            this.Parameters = parameters;
        }

        public EventData(Enum eventCode, Dictionary<byte, object> parameters)
            : this((byte)(object)eventCode, parameters)
        { }

        public EventData() { }

        public EventData(byte evCode)
        {
            Code = evCode;
        }

        public EventData(Enum evCode)
            : this((byte)(object)evCode)
        { }

        public override byte ServiceCode
        {
            get { return (byte)ServiceType.EventData; }
        }

        protected override string GetNameOfCodeType()
        {
            return "EventCode";
        }
    }
}
