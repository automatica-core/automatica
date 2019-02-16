using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Driver.LeanMode
{
    public interface ILearnMode
    {
        Task NotifyLearnNode(string name, string description, NodeInstance self, IList<NodeTemplate> templates, IList<PropertyInstance> propertyInstances);
    }
}
