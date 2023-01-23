using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi
{
    public abstract class BaseController : Controller
    {
        public AutomaticaContext DbContext { get; }
        protected BaseController(AutomaticaContext dbContext)
        {
            DbContext = dbContext;
        }

        public bool IsUserInGroup(Guid? groupId)
        {
            if(groupId == null)
            {
                return true;
            }
            var claim = User.Claims.SingleOrDefault(a => a.Type == UserGroup.ClaimType && a.Value == groupId.ToString());
            return claim != null;
        }
    }
}
