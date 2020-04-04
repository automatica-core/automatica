using System;
using System.Collections.Generic;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class NodeTemplate : TypedObject
    {
        public NodeTemplate()
        {
            PropertyTemplate = new List<PropertyTemplate>();
        }

        public Guid ObjId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public Guid NeedsInterface2InterfacesType { get; set; }
        public Guid ProvidesInterface2InterfaceType { get; set; }
        public bool IsDeleteable { get; set; }
        public bool DefaultCreated { get; set; }
        public bool IsReadable { get; set; }
        public bool IsReadableFixed { get; set; }
        public bool IsWriteable { get; set; }
        public bool IsWriteableFixed { get; set; }
        public long This2NodeDataType { get; set; }
        public int MaxInstances { get; set; }
        public bool? IsAdapterInterface { get; set; }

        public string NameMeta { get; set; }

        public Guid This2DefaultMobileVisuTemplate { get; set; }
        public Guid FactoryReference { get; set; }

        public InterfaceType NeedsInterface2InterfacesTypeNavigation { get; set; }
        public InterfaceType ProvidesInterface2InterfaceTypeNavigation { get; set; }
        public NodeDataType This2NodeDataTypeNavigation { get; set; }
        public List<PropertyTemplate> PropertyTemplate { get; set; }

        public VisuObjectTemplate THis2DefaultMobileVisuTemplateNavigation { get; set; }
    }
}
