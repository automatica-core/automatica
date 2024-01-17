﻿using System;

namespace Automatica.Core.Model.Models.User
{
   public class UserGroup2Role : TypedObject
    {
        public Guid This2UserGroup { get; set; }
        public Guid This2Role { get; set; }

        public UserGroup This2UserGroupNavigation { get; set; }

        public Role This2RoleNavigation { get; set; }
    }
}
