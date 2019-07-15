using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Abstraction.Plugins;

namespace Automatica.Core.Runtime.Abstraction
{
    internal interface INodeInstanceStore : IStore<NodeInstance>
    {
    }
}
