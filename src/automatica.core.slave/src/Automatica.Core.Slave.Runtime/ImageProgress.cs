using Docker.DotNet.Models;
using System;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Slave.Runtime
{
    internal class ImageProgress : IProgress<JSONMessage>
    {
        private readonly ILogger _logger;

        public ImageProgress(ILogger logger)
        {
            _logger = logger;
        }

        public void Report(JSONMessage value)
        {
            _logger.LogDebug($"{value.Status}:{value.ID}:{value.ProgressMessage}");
        }
    }
}
