using System;

namespace Automatica.Core.Model.Models.User
{
    public class Priviledge2Role
    {
        public Guid This2Priviledge { get; set; }
        public Guid This2Role { get; set; }


        public Priviledge This2PriviledgeNavigation { get; set; }
        public Role This2RoleNavigation { get; set; }
    }
}
