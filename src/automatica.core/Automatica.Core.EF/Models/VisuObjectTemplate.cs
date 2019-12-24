using System;
using System.Collections.Generic;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class VisuObjectTemplate : TypedObject
    {
        public VisuObjectTemplate()
        {
            PropertyTemplate = new List<PropertyTemplate>();
        }

        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public string Group { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public float? MaxWidth { get; set; }
        public float? MaxHeight { get; set; }

        public float? MinWidth { get; set; }
        public float? MinHeight { get; set; }

        public long This2VisuPageType { get; set; }

        public bool IsVisibleForUser { get; set; }

        public VisuPageType This2VisuPageTypeNavigation { get; set; }

        public List<PropertyTemplate> PropertyTemplate { get; set; }
    }
}
