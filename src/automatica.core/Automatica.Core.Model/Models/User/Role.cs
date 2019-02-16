using System;
using System.Collections.Generic;

namespace Automatica.Core.Model.Models.User
{
    public class Role : TypedObject
    {
        public const string AdminRole = "administrator";
        public const string ViewerRole = "viewer";
        public const string VisuRole = "visu";


        public Role()
        {
            InverseThis2Priviledges = new List<Priviledge2Role>();
        }

        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string Key { get; set; }

        public bool IsDeleteable { get; set; }

        public List<Priviledge2Role> InverseThis2Priviledges { get; set; }

    }
}