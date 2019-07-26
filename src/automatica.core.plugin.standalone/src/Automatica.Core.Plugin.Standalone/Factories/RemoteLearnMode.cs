using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Core.Plugin.Standalone.Abstraction;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    internal class RemoteLearnMode : ILearnMode
    {
        private readonly IDriverConnection _connection;

        public RemoteLearnMode(IDriverConnection connection)
        {
            _connection = connection;
        }

        public async Task NotifyLearnNode(string name, string description, NodeInstance self, IList<NodeTemplate> templates, IList<PropertyInstance> propertyInstances)
        {
            dynamic dataObj = new ExpandoObject();

            dataObj.Name = name;
            dataObj.Description = description;
            dataObj.Self = self;
            dataObj.Templates = templates;
            dataObj.PropertyInstances = propertyInstances;


            await _connection.Send("notifyLearnMode", dataObj);
        }
    }
}
