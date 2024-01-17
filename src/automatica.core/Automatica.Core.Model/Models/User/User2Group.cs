using System;
namespace Automatica.Core.Model.Models.User
{
    public class User2Group : TypedObject
    {
        public Guid This2User { get; set; }
        public Guid This2UserGroup { get; set; }
        public User This2UserNavigation { get; set; }
        public UserGroup This2UserGroupNavigation { get; set; }
    }
}
