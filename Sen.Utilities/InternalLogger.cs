using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sen.Utilities
{
    public static class InternalLogger
    {
        public static ILoggerFactory LoggerFactory { get; set; }

        public static ILogger GetLogger<T>()
        {
            return LoggerFactory.CreateLogger<T>();
        }

        public static ILogger GetLogger(string name)
        {
            return LoggerFactory.CreateLogger(name);
        }
    }
}
