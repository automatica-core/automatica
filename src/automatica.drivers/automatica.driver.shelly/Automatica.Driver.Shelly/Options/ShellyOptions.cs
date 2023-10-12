using System;

namespace Automatica.Driver.Shelly.Options
{
    public class ShellyOptions : IShellyCommonOptions
    {
        public Uri ServerUri { get; set; }
        public TimeSpan? DefaultTimeout { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public ShellyOptions()
        {
            DefaultTimeout = null;
        }
    }
}