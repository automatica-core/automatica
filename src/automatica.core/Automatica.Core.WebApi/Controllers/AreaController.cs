using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.LinqExtensions;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.Internals.Areas;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;
using P3.Knx.Core.Ets;

[assembly:InternalsVisibleTo("Automatica.Core.WebApi.Tests")]

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/areas")]

    public class AreaController : BaseController
    {
        private readonly IAreaCache _areaCache;
        private readonly IAreaTemplateCache _areaTemplateCache;
        private readonly ILogger<AreaController> _logger;

        public AreaController(AutomaticaContext dbContext, IAreaCache areaCache, IAreaTemplateCache areaTemplateCache, ILogger<AreaController> logger) : base(dbContext)
        {
            _areaCache = areaCache;
            _areaTemplateCache = areaTemplateCache;
            _logger = logger;
        }

        [HttpGet]
        [Route("templates")]
        [Authorize(Policy = Role.ViewerRole)]
        public IEnumerable<AreaTemplate> GetTemplates()
        {
            return _areaTemplateCache.All();
        }

        [HttpPost]
        [Route("{parentObjId}/etsImport")]
        public async Task<IEnumerable<AreaInstance>> Post(Guid parentObjId)
        {
            // full path to file in temp location
            var parentInstance = await DbContext.AreaInstances.SingleOrDefaultAsync(a => a.ObjId == parentObjId);

            if (parentInstance == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return new List<AreaInstance>(); 
            }
            var myFile = Request.Form.Files[0]; 
            var password = Request.Headers["password"].ToString();

            try
            {
                var response =  (await ProcessFile(password, myFile)).ToList();

                response.First().This2Parent = null;
                var flatten = response.Flatten(a => a.InverseThis2ParentNavigation).ToList();

                foreach (var area in flatten)
                {
                    area.InverseThis2ParentNavigation = null;
                    area.This2ParentNavigation = null;
                    area.This2AreaTemplateNavigation = null;
                }

                return flatten;
            }
            catch
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

            return new List<AreaInstance>();
        }

        internal async Task<IEnumerable<AreaInstance>> ProcessFile(string password, IFormFile formFile)
        {
            var targetLocation = ServerInfo.GetTempPath();
            var path = Path.Combine(targetLocation, formFile.FileName);

            // Uncomment to save the file
            await using (var fileStream = System.IO.File.Create(path))
            {
                await formFile.CopyToAsync(fileStream);
            }


            var etsProject = new EtsProjectParser().ParseEtsFile(path, password, GroupAddressStyle.ThreeLevel);
            var instances = new List<AreaInstance>();
            foreach (var b in etsProject.Buildings)
            {
                var bInstance = CreateAreaInstance(null, b);
                instances.Add(bInstance);
            }

            return instances;
        }

        private AreaInstance CreateAreaInstance(AreaInstance parent, EtsBuildingPart building)
        {
            Guid typeGuid;
            var icon = AreaTemplateAttribute.GetAttributeFromEnum(AreaTemplates.BuildingPart).Icon;
            if (building is EtsBuilding)
            {
                typeGuid = AreaTemplateAttribute.GetFromEnum(AreaTemplates.Building);
            }
            else
            {
                switch (building.EtsBuildingType)
                {
                    case EtsBuildingType.BuildingPart:
                        typeGuid = AreaTemplateAttribute.GetFromEnum(AreaTemplates.BuildingPart);
                        icon = AreaTemplateAttribute.GetAttributeFromEnum(AreaTemplates.BuildingPart).Icon;
                        break;
                    case EtsBuildingType.Floor:
                    case EtsBuildingType.Corridor:
                        typeGuid = AreaTemplateAttribute.GetFromEnum(AreaTemplates.Hallway);
                        icon = AreaTemplateAttribute.GetAttributeFromEnum(AreaTemplates.Hallway).Icon;
                        break;
                    case EtsBuildingType.Stairway:
                        typeGuid = AreaTemplateAttribute.GetFromEnum(AreaTemplates.Staircase);
                        icon = AreaTemplateAttribute.GetAttributeFromEnum(AreaTemplates.Staircase).Icon;
                        break;
                    default:
                        typeGuid = AreaTemplateAttribute.GetFromEnum(AreaTemplates.Room);
                        icon = AreaTemplateAttribute.GetAttributeFromEnum(AreaTemplates.Room).Icon;
                        break;
                }
            }

            var instance = new AreaInstance
            {
                ObjId = Guid.NewGuid(),
                Name = building.Name,
                Description = building.Description,
                Icon = icon,
                This2AreaTemplate = typeGuid,
                This2Parent = parent?.ObjId,
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now
            };

            foreach (var part in building.Children)
            {
                var partInstance = CreateAreaInstance(instance, part);
                instance.InverseThis2ParentNavigation.Add(partInstance);
            }



            return instance;
        }

        [HttpPost]
        [Authorize]
        [Authorize(Policy = Role.AdminRole)]
        [Route("add")]
        public async Task<IEnumerable<AreaInstance>> AddAreaInstances([FromBody]IEnumerable<AreaInstance> instances)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                await DbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var instance in instances)
                    {
                        instance.This2AreaTemplateNavigation = null;
                        instance.InverseThis2ParentNavigation = null;
                        instance.This2ParentNavigation = null;

                        instance.CreatedAt = DateTimeOffset.Now;
                        instance.ModifiedAt = DateTimeOffset.Now;
                        await DbContext.AreaInstances.AddAsync(instance);
                    }

                    await DbContext.SaveChangesAsync();

                }
                finally
                {
                    await DbContext.Database.CommitTransactionAsync();
                    _areaCache.Clear();
                }

                return GetInstances();
            });
        }

        [HttpGet]
        [Authorize]
        [Authorize(Policy = Role.ViewerRole)]
        public IEnumerable<AreaInstance> GetInstances()
        {
            return _areaCache.All().Where(a => IsUserInGroup(a.This2UserGroup));
        }

        [HttpGet("all")]
        [Authorize]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<AreaInstance> GetAllInstances()
        {
            return _areaCache.All();
        }

        private async Task SaveAreaInstance(AreaInstance instance)
        {
            var existingArea = await DbContext.AreaInstances.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);
            instance.InverseThis2ParentNavigation = null;
            instance.This2ParentNavigation = null;
            instance.This2UserGroupNavigation = null;
            instance.This2AreaTemplateNavigation = null;
            instance.ModifiedAt = DateTimeOffset.Now;

            if (existingArea == null)
            {
                instance.CreatedAt = DateTimeOffset.Now;
                await DbContext.AreaInstances.AddAsync(instance);
                _areaCache.Add(instance.ObjId, instance);
            }
            else
            {
                DbContext.Entry(instance).State = EntityState.Modified;
                DbContext.AreaInstances.Update(instance);
                _areaCache.Update(instance.ObjId, instance);
            }

        }

        private async Task RemoveInstanceRecursive(AreaInstance parent)
        {
            var area = _areaCache.GetSingle(DbContext, parent.ObjId);
            foreach (var child in area.InverseThis2ParentNavigation)
            {
                await RemoveInstanceRecursive(child);

                child.InverseThis2ParentNavigation = null;
                child.This2AreaTemplateNavigation = null;
                child.This2ParentNavigation = null;

                DbContext.Remove(child);
                _areaCache.Remove(child.ObjId);
            }

            parent.This2AreaTemplateNavigation = null;
            parent.This2ParentNavigation = null;
            parent.InverseThis2ParentNavigation = null;

            DbContext.Remove(parent);
            _areaCache.Remove(parent.ObjId);

        }

        [HttpDelete("{areaInstanceId}")]
        [Authorize]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<AreaInstance>> RemoveInstance(Guid areaInstanceId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var area = _areaCache.GetSingle(DbContext, areaInstanceId);

                    await RemoveInstanceRecursive(area);

                    await DbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                    throw;
                }
                return GetInstances();
            });
        }

        [HttpPut]
        [Authorize]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<AreaInstance>> SaveInstance([FromBody] AreaInstance area)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    await SaveAreaInstance(area);

                    await DbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                    throw;
                }

                return GetInstances();
            });
        }

        [HttpPost]
        [Authorize]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<AreaInstance>> SaveInstances([FromBody]IEnumerable<AreaInstance> areas)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var area in areas)
                    {
                        await SaveAreaInstance(area);
                    }

                    await DbContext.SaveChangesAsync();

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                    throw;
                }
                return GetInstances();
            });
        }

       
    }
}
