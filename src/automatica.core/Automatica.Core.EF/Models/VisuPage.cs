using System;
using System.Collections.Generic;
using Automatica.Core.Model;
using Automatica.Core.Model.Models.User;


namespace Automatica.Core.EF.Models
{
    public class VisuPage : TypedObject
    {
        public VisuPage()
        {
            VisuObjectInstances = new List<VisuObjectInstance>();
        }

        public Guid ObjId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public long This2VisuPageType { get; set; }

        public bool DefaultPage { get; set; }

        public double Height { get; set; }
        public double Width { get; set; }

        public Guid? This2UserGroup { get; set; }
        public bool IsFavorite { get; set; }
        public int Rating { get; set; }


        public UserGroup This2UserGroupNavigation { get; set; }

        public VisuPageType This2VisuPageTypeNavigation { get; set; }

        public List<VisuObjectInstance> VisuObjectInstances { get; set; }

    }
}
