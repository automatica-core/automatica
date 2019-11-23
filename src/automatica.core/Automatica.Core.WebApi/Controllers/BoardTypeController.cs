using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/boardType")]
    public class BoardTypeController : BaseController
    {
        public BoardTypeController(AutomaticaContext db)
            : base(db)
        {

        }

        [HttpGet]
        [Authorize(Policy = Role.ViewerRole)]
        public BoardType Get()
        {
            var boardType =  DbContext.BoardTypes.Include(a => a.BoardInterface).ThenInclude(b => b.This2InterfaceTypeNavigation).AsNoTracking().FirstOrDefault();
            return boardType;
        }
    }
}
