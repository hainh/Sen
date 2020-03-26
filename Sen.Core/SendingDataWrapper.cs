using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public class SendingDataWrapper
    {
        public object Data;

        public SendParameters SendParameters;

        public SendingDataWrapper(object data, SendParameters sendParameters)
        {
            Data = data;
            SendParameters = sendParameters;
        }
    }
}
