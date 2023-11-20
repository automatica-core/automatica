using System;
using System.Collections.Generic;
using Automatica.Core.Model.Models.User;
using Newtonsoft.Json;


namespace Automatica.Core.EF.Models
{
    public partial class VisuObjectInstance
    {
        public VisuObjectInstance()
        {
            PropertyInstance = new List<PropertyInstance>();
        }

        public Guid ObjId { get; set; }
        public Guid This2VisuObjectTemplate { get; set; }
        public Guid This2VisuPage { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public float Height { get; set; }
        public float Width { get; set; }


        public Guid? This2UserGroup { get; set; }
        public bool IsFavorite { get; set; }
        public int Rating { get; set; }

        public UserGroup This2UserGroupNavigation { get; set; }

        [JsonIgnore]
        public VisuPage This2VisuPageNavigation { get; set; }
       
        public VisuObjectTemplate This2VisuObjectTemplateNavigation { get; set; }

        public List<PropertyInstance> PropertyInstance { get; set; }
    }
}
