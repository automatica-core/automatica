using System;

namespace Automatica.Driver.Shelly.Options
{
    public class Shelly1Options : IShellyCommonOptions
    {
        public Uri ServerUri { get; set; }
        public TimeSpan? DefaultTimeout { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public Shelly1Options()
        {
            DefaultTimeout = null;
        }
    }
}