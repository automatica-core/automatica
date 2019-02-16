using System;
using MessagePack;

namespace Automatica.Core.Model.Models.User
{
    public class User2Group : TypedObject
    {
        public Guid This2User { get; set; }
        public Guid This2UserGroup { get; set; }

        [IgnoreMember]
        public User This2UserNavigation { get; set; }
        [IgnoreMember]
        public UserGroup This2UserGroupNavigation { get; set; }
    }
}
