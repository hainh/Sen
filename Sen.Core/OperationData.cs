using Senla.Core.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public class OperationData : AbstractDataContainer
    {

        public OperationData(byte opCode, Dictionary<byte, object> parameters)
        {
            Code = opCode;
            this.Parameters = parameters;
        }

        public OperationData(Enum opCode, Dictionary<byte, object> parameters)
            : this((byte)(object)opCode, parameters)
        { }

        public OperationData() { }

        public OperationData(byte opCode)
        {
            Code = opCode;
        }

        public OperationData(Enum opCode)
            : this((byte)(object)opCode)
        { }

        public override byte ServiceCode
        {
            get { return (byte)ServiceType.OperationData; }
        }

        protected override string GetNameOfCodeType()
        {
            return "OperationCode";
        }
    }
}
