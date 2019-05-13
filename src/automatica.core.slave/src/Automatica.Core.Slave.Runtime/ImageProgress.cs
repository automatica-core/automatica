using Docker.DotNet.Models;
using System;

namespace Automatica.Core.Slave.Runtime
{
    internal class ImageProgress : IProgress<JSONMessage>
    {
        public void Report(JSONMessage value)
        {
            Console.WriteLine(value.ProgressMessage);
        }
    }
}
