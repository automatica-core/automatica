using System;
using MessagePack;

namespace Automatica.Core.Model.Models.User
{
    public class User2Role : TypedObject
    {
        public Guid This2User { get; set; }
        public Guid This2Role { get; set; }


        [IgnoreMember]
        public User This2UserNavigation { get; set; }
        public Role This2RoleNavigation { get; set; }
    }
}
