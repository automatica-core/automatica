using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;

namespace Automatica.Core.CI.CreateDatabase
{
    public class EmptyLearnMode :ILearnMode
    {
        public Task NotifyLearnNode(string name, string description, NodeInstance self, IList<NodeTemplate> templates, IList<PropertyInstance> propertyInstances)
        {
            return Task.CompletedTask;
        }
    }
}
