using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Plugin.Standalone.Abstraction
{
    public interface IConnection
    {
        string MasterAddress { get; }
        string Username { get; }
        string Password { get; }

        ILogger Logger { get; }

    }
}
