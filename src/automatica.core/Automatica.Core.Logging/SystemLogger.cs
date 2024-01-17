using Microsoft.Extensions.Logging;

namespace Automatica.Core.Logging
{
    public class SystemLogger
    {
        private static ILogger? _instance;
        public static ILogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CoreLoggerFactory.GetLogger(null, null, "system");
                }
                return _instance;
            }
        }
    }
}
