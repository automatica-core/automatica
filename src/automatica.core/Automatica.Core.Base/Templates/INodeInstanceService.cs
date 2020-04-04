using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.Templates
{
    public interface INodeInstanceService
    {
        NodeInstance CreateNodeInstance(string locale, NodeInstance parent, NodeTemplate nodeTemplate);
        NodeInstance CreateNodeInstance(string locale, NodeTemplate nodeTemplate);

        NodeTemplate GetTemplateById(Guid id);
        ICollection<NodeTemplate> GetTemplatesById(params Guid[] ids);
        NodeTemplate GetTemplateByKey(string key);
    }
}
