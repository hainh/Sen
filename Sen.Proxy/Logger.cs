using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sen.Proxy
{
    public class Logger
    {
        public static ILoggerFactory LoggerFactory { get; }

        static Logger()
        {
            LoggerFactory = new NLogLoggerFactory();
            Utilities.InternalLogger.LoggerFactory = LoggerFactory;
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory = LoggerFactory;
        }
    }
}
