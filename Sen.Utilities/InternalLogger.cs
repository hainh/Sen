using Microsoft.Extensions.Logging;

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
