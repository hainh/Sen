using System;

namespace Senla.Core.Log
{
    internal interface ILogger
    {
        bool IsErrorEnabled { get; set; }
        bool IsWarnEnabled { get; set; }

        void Error(Exception exceptionObject);
        void Warn(string v);
    }
}