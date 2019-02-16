using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Automatica.Push.LearnMode
{
    public class LearnMode : ILearnMode
    {
        private readonly IHubContext<DataHub> _dataHub;

        public LearnMode(IHubContext<DataHub> dataHub)
        {
            _dataHub = dataHub;
        }
        public async Task NotifyLearnNode(string name, string description, NodeInstance self, IList<NodeTemplate> templates, IList<PropertyInstance> propertyInstances)
        {
            await _dataHub.Clients.Group(self.ObjId.ToString()).SendAsync("NotifyLearnMode", name, description, templates, propertyInstances);
        }
    }
}
