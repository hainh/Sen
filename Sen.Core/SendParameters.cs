using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Core
{
    public class SendParameters
    {
        public bool Encrypted { get; set; }

        public bool Flush { get; set; }

        public bool Reliable { get; set; }

        public byte ChannelId { get; set; }

        public SendParameters()
        {
            Encrypted = false;
            Flush = true;
            Reliable = true;
            ChannelId = 0;
        }
    }
}
