using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("slave")]
    public class SlaveController : BaseController
    {
        public SlaveController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpGet]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<Slave> GetSlaves()
        {
            return DbContext.Slaves.AsNoTracking();
        }

        [HttpPost]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<Slave>> SaveInstances([FromBody]IList<Slave> instances)
        {
            var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var instance in instances)
                {
                    var existingArea = await DbContext.Slaves.SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);

                    if (existingArea == null)
                    {
                        await DbContext.Slaves.AddAsync(instance);
                    }
                    else
                    {
                        DbContext.Entry(existingArea).State = EntityState.Detached;
                        DbContext.Slaves.Update(instance);
                    }
                }

                var removedNodes = from c in DbContext.Slaves
                                   where !(from o in instances select o.ObjId).Contains(c.ObjId)
                                   select c;
                var removedList = removedNodes.ToList();
                DbContext.RemoveRange(removedList);

                await DbContext.SaveChangesAsync();
                transaction.Commit();
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                transaction.Rollback();
            }

            return GetSlaves();
        }

    }
}
