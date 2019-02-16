using System;
using System.Collections.Generic;

namespace Automatica.Core.Model.Models.User
{
    public class Priviledge : TypedObject
    {
        public Priviledge()
        {
            InverseThis2Roles = new List<Priviledge2Role>();
        }

        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string Key { get; set; }


        public List<Priviledge2Role> InverseThis2Roles { get; set; }
    }
}
