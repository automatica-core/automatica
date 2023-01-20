using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.LinqExtensions;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Areas;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Ets;

[assembly:InternalsVisibleTo("Automatica.Core.WebApi.Tests")]

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/areas")]

    public class AreaController : BaseController
    {
        private readonly IAreaCache _areaCache;
        private readonly IAreaTemplateCache _areaTemplateCache;

        public AreaController(AutomaticaContext dbContext, IAreaCache areaCache, IAreaTemplateCache areaTemplateCache) : base(dbContext)
        {
            _areaCache = areaCache;
            _areaTemplateCache = areaTemplateCache;
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

            try
            {
                return await ProcessFile(parentInstance, myFile);
            }
            catch
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

            return new List<AreaInstance>();
        }

        internal async Task<IEnumerable<AreaInstance>> ProcessFile(AreaInstance parentInstance, IFormFile formFile)
        {
            var targetLocation = ServerInfo.GetTempPath();
            var path = Path.Combine(targetLocation, formFile.FileName);

            // Uncomment to save the file
            await using (var fileStream = System.IO.File.Create(path))
            {
                await formFile.CopyToAsync(fileStream);
            }


            var etsProject = new EtsProjectParser().ParseEtsFile(path, GroupAddressStyle.ThreeLevel);

            foreach (var b in etsProject.Buildings)
            {
                var bInstance = CreateAreaInstance(parentInstance, b);

                parentInstance.InverseThis2ParentNavigation.Add(bInstance);
            }

            return new List<AreaInstance>
            {
                parentInstance
            };
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
                This2Parent = parent.ObjId
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
            await DbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var instance in instances)
                {
                    instance.InverseThis2ParentNavigation = null;
                    instance.This2ParentNavigation = null;
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
        }

        [HttpGet]
        [Authorize]
        [Authorize(Policy = Role.ViewerRole)]
        public IEnumerable<AreaInstance> GetInstances()
        {
            return _areaCache.All().Where(a => IsUserInGroup(a.This2UserGroup));
        }

        private async Task SaveAreaInstanceRec(AreaInstance instance)
        {
            var existingArea = await DbContext.AreaInstances.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);
            var childrens = instance.InverseThis2ParentNavigation ?? new List<AreaInstance>(); 

            instance.InverseThis2ParentNavigation = null;
            instance.This2ParentNavigation = null;
            instance.This2UserGroupNavigation = null;
            instance.This2AreaTemplateNavigation = null;

            if (existingArea == null)
            {
                await DbContext.AreaInstances.AddAsync(instance);
            }
            else
            {
                DbContext.Entry(instance).State = EntityState.Modified;
                DbContext.AreaInstances.Update(instance);
            }

            foreach (var child in childrens)
            {
                await SaveAreaInstanceRec(child);
            }
        }

        [HttpPost]
        [Authorize]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<AreaInstance>> SaveInstances([FromBody]IEnumerable<AreaInstance> areas)
        {
            var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var areaInstances = areas as AreaInstance[] ?? areas.ToArray();
                var flatList = areaInstances.Flatten(a => a.InverseThis2ParentNavigation).ToList();
                foreach (var area in areaInstances)
                {
                    await SaveAreaInstanceRec(area);
                }

                await DbContext.SaveChangesAsync();



                var removedNodes = from c in DbContext.AreaInstances.AsNoTracking()
                    where !(from o in flatList select o.ObjId).Contains(c.ObjId)
                    select c;
                var removedAreasList = removedNodes.ToList();
                DbContext.RemoveRange(removedAreasList);
                removedAreasList.ForEach(a => { _areaCache.Remove(a.ObjId); });


                await DbContext.SaveChangesAsync(true);
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                _areaCache.Clear();
            }

            return GetInstances();
        }

       
    }
}
