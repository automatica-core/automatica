using System;
using System.Collections.Generic;

namespace Automatica.Core.Model.Models.User
{
    public class UserGroup : TypedObject
    {
        public const string ClaimType = "UserGroup";
        public UserGroup()
        {
            InverseThis2Users = new List<User2Group>();
            InverseThis2Roles = new List<UserGroup2Role>();
        }

        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<User2Group> InverseThis2Users { get; set; }
        public List<UserGroup2Role> InverseThis2Roles { get; set; }
    }
}
