using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Automatica.Push.LearnMode
{
    public class LearnMode : ILearnMode
    {
        private readonly IHubContext<DataHub> _dataHub;
        private readonly ILogger<LearnMode> _logger;

        public LearnMode(IHubContext<DataHub> dataHub, ILogger<LearnMode> logger)
        {
            _dataHub = dataHub;
            _logger = logger;
        }
        public async Task NotifyLearnNode(string name, string description, NodeInstance self, IList<NodeTemplate> templates, IList<PropertyInstance> propertyInstances)
        {
            _logger.LogDebug($"{nameof(NotifyLearnNode)} for {name}-{description} nodeInstance {self.Name} ({self.ObjId}) with templateCount of {templates.Count} and propertyInstanceCount of {propertyInstances.Count}");
            await _dataHub.Clients.Group(self.ObjId.ToString()).SendAsync("NotifyLearnMode", name, description, templates, propertyInstances);
        }
    }
}
