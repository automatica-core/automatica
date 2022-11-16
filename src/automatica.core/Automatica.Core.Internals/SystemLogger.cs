using Automatica.Core.Internals.Logger;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals
{
    public class SystemLogger
    {
        private static ILogger _instance;
        public static ILogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CoreLoggerFactory.GetLogger("system");
                }
                return _instance;
            }
        }

        private static ILogger _mqtt;
        public static ILogger Mqtt
        {
            get
            {
                if (_mqtt == null)
                {
                    _mqtt = CoreLoggerFactory.GetLogger("mqtt");
                }
                return _mqtt;
            }
        }



        private static ILogger _dispatcherLogger;
        public static ILogger DispatcherLogger
        {
            get
            {
                if (_dispatcherLogger == null)
                {
                    _dispatcherLogger = CoreLoggerFactory.GetLogger("Dispatcher");
                }
                return _dispatcherLogger;
            }
        }

    }
}
