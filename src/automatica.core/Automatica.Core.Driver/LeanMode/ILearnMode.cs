using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Driver.LeanMode
{
    public class LearnModeDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public NodeInstance Self { get; set; }
        public IList<NodeTemplate> Templates { get; set; }
        public IList<PropertyInstance> PropertyInstances { get; set; }
    }

    public interface ILearnMode
    {
        Task NotifyLearnNode(string name, string description, NodeInstance self, IList<NodeTemplate> templates, IList<PropertyInstance> propertyInstances);
    }
}
