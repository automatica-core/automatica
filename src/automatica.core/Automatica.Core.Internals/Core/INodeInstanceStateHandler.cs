using System;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Core
{
    public interface INodeInstanceStateHandler
    {
        NodeInstanceState GetNodeInstanceState(Guid id);
    }
}
