using System;
using System.Collections.Generic;
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
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public string UserName { get; set; }
        public string Description { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Password { get; set; }

        public string PasswordConfirm { get; set; }

        [JsonIgnore]
        public string Salt { get; set; }

        public string Token { get; set; }

        /// <summary>
        /// The user must set a new password before anything else (new installation, default password, reset by an administrator).
        /// While set, the token of the user has no roles.
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>
        /// Changes with every password change, tokens with another stamp are rejected
        /// </summary>
        [JsonIgnore]
        public string SecurityStamp { get; set; }


        public List<User2Group> InverseThis2UserGroups { get; set; }
        public List<User2Role> InverseThis2Roles { get; set; }

    }
}
