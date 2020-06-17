using System;

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
