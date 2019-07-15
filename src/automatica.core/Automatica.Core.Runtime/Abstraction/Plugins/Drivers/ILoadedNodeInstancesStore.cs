using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Drivers
{
    internal interface ILoadedNodeInstancesStore : IStore<NodeInstance>, INodeInstanceStateHandler
    {
    }
}
