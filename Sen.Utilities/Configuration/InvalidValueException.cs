using System;
using System.Collections.Generic;
using System.Text;

namespace Sen.Utilities.Configuration
{
    [Serializable]
    public class InvalidValueException : Exception
    {
        public InvalidValueException(string message)
            : base(message)
        {
        }
    }
}
