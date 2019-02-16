using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

namespace Automatica.Core.Model.Models.User
{
    public partial class User : TypedObject
    {
        public User()
        {
            InverseThis2UserGroups = new List<User2Group>();
            InverseThis2Roles = new List<User2Role>();

            Description = "";
        }

        public Guid ObjId { get; set; }
        public string UserName { get; set; }
        public string Description { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Password { get; set; }

        public string PasswordConfirm { get; set; }

        [JsonIgnore, IgnoreMember]
        public string Salt { get; set; }

        public string Token { get; set; }


        public List<User2Group> InverseThis2UserGroups { get; set; }
        public List<User2Role> InverseThis2Roles { get; set; }

    }
}
