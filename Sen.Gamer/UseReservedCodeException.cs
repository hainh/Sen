using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senla.Gamer
{
    public class UseReservedCodeException : SystemException
    {
        public UseReservedCodeException(string message)
            : base(message)
        {

        }
    }
}
