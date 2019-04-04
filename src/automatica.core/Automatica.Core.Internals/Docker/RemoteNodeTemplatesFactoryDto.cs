using Automatica.Core.EF.Models;
using System.Collections.Generic;

namespace Automatica.Core.Internals.Docker
{
    public class RemoteNodeTemplatesFactoryDto
    {
        public IReadOnlyCollection<NodeTemplate> NodeTemplates { get; set; }
        public IReadOnlyCollection<PropertyTemplate> PropertyTemplates { get; set; }
        public IReadOnlyCollection<PropertyTemplateConstraint> PropertyTemplateConstraints { get; set; }
        public IReadOnlyCollection<PropertyTemplateConstraintData> PropertyTemplateConstraintData { get; set; }
        public IReadOnlyCollection<Setting> Settings { get; set; }
        public IReadOnlyCollection<InterfaceType> InterfaceTypes { get; set; }

    }
}
