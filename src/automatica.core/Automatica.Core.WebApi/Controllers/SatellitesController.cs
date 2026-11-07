using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Automatica.Core.Internals.Core;
using Automatica.Core.Base.Common;
using Automatica.Core.Runtime.Core;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/satellite")]
    public class SatellitesController : BaseController
    {
        private readonly ILogger<SatellitesController> _logger;
        private readonly IRemoteServerHandler _remoteServerHandler;

        public SatellitesController(ILogger<SatellitesController> logger, AutomaticaContext dbContext, IRemoteServerHandler remoteServerHandler) : base(dbContext)
        {
            _logger = logger;
            _remoteServerHandler = remoteServerHandler;
        }

        [HttpGet]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<Slave> GetAll()
        {
            var slaves = DbContext.Slaves.AsNoTracking().ToList();

            var connectedSlaves = _remoteServerHandler.GetConnectedClients();

            foreach(var slave in slaves)
            {
                if(slave.ObjId == ServerInfo.SelfSlaveGuid)
                {
                    slave.Connected = true;
                    continue;
                }
                // the broker lists the client ids of the connected satellites
                if(connectedSlaves.Contains(slave.ClientId))
                {
                    slave.Connected = true;
                }
            }

            // the client key is write only (the broker stores a hash of it): an empty key keeps the stored one on save
            foreach (var slave in slaves)
            {
                slave.ClientKey = null;
            }

            return slaves;
        }

        [HttpPost]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<ActionResult<IEnumerable<Slave>>> SaveInstances([FromBody]IList<Slave> instances)
        {
            if (instances == null)
            {
                return Ok(GetAll());
            }

            // a new key is typed in plain text, only its hash is stored (the broker verifies against the hash)
            foreach (var instance in instances)
            {
                if (string.IsNullOrEmpty(instance.ClientKey) || SatelliteClientKey.IsHashed(instance.ClientKey))
                {
                    continue;
                }

                if (instance.ClientKey.Length < SatelliteClientKey.MinimumKeyLength)
                {
                    return BadRequest(new WebApiException("SATELLITE_KEY_TOO_SHORT", ExceptionSeverity.Warning).ToJson());
                }

                instance.ClientKey = SatelliteClientKey.Hash(instance.ClientKey);
            }

            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var instance in instances)
                    {
                        var existingArea = await DbContext.Slaves.SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);

                        instance.ModifiedAt = DateTimeOffset.Now;
                        if (existingArea == null)
                        {
                            instance.CreatedAt = DateTimeOffset.Now;
                            await DbContext.Slaves.AddAsync(instance);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(instance.ClientKey))
                            {
                                // not sent back by GetAll: keep the stored key
                                instance.ClientKey = existingArea.ClientKey;
                            }

                            DbContext.Entry(existingArea).State = EntityState.Detached;
                            DbContext.Slaves.Update(instance);
                        }
                    }

                    // the UI always posts the complete list: an empty list is treated as "nothing to delete"
                    // and the own (local) satellite is never deleted
                    if (instances.Count > 0)
                    {
                        var postedIds = instances.Select(o => o.ObjId).ToList();
                        var selfSlaveId = ServerInfo.SelfSlaveGuid;
                        var removedList = DbContext.Slaves
                            .Where(c => !postedIds.Contains(c.ObjId) && c.ObjId != selfSlaveId)
                            .ToList();
                        DbContext.RemoveRange(removedList);
                    }

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    throw;
                }
            });

            return Ok(GetAll());
        }

    }
}
