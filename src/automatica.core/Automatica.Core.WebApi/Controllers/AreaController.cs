using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.LinqExtensions;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Areas;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Ets;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("areas")]

    public class AreaController : BaseController
    {
        public AreaController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpGet]
        [Route("templates")]
        [Authorize(Policy = Role.ViewerRole)]
        public IEnumerable<AreaTemplate> GetTemplates()
        {
            return DbContext.AreaTemplates;
        }

        [HttpPost]
        [Route("{parentObjId}/etsImport")]
        public async Task<IEnumerable<AreaInstance>> Post(Guid parentObjId)
        {
            // full path to file in temp location

            var parentInstance = await DbContext.AreaInstances.SingleOrDefaultAsync(a => a.ObjId == parentObjId);

            if (parentInstance == null)
            {
                return new List<AreaInstance>(); //TODO error handling
            }
            var myFile = Request.Form.Files[0];
            var targetLocation = Path.GetTempPath();

            try
            {
                var path = Path.Combine(targetLocation, myFile.FileName);

                // Uncomment to save the file
                using (var fileStream = System.IO.File.Create(path))
                {
                    myFile.CopyTo(fileStream);
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
            catch
            {
                Response.StatusCode = 400;
            }

            return new List<AreaInstance>();
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
                        typeGuid = AreaTemplateAttribute.GetFromEnum(AreaTemplates.Hallway);
                        icon = AreaTemplateAttribute.GetAttributeFromEnum(AreaTemplates.Hallway).Icon;
                        break;
                    case EtsBuildingType.Room:
                        typeGuid = AreaTemplateAttribute.GetFromEnum(AreaTemplates.Room);
                        icon = AreaTemplateAttribute.GetAttributeFromEnum(AreaTemplates.Room).Icon;
                        break;
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
                DbContext.Database.CommitTransaction();
            }
            return GetInstances();
        }

        [HttpGet]
        [Authorize]
        [Authorize(Policy = Role.ViewerRole)]
        public IEnumerable<AreaInstance> GetInstances()
        {
            var rootItems = DbContext.AreaInstances.Where(a => a.This2Parent == null).Where(a => IsUserInGroup(a.This2UserGroup));
            var items = new List<AreaInstance>();

            foreach (var root in rootItems)
            {
                items.Add(RecursiveLoad(root, DbContext));
            }

            return items;
        }

        private async Task SaveAreaInstanceRec(AreaInstance instance)
        {
            var existingArea = await DbContext.AreaInstances.SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);

            if (existingArea == null)
            {
                await DbContext.AreaInstances.AddAsync(instance);
            }
            else
            {
                DbContext.Entry(existingArea).State = EntityState.Detached;
                DbContext.Update(instance);
            }

            foreach (var child in instance.InverseThis2ParentNavigation)
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
                foreach (var area in areaInstances)
                {
                    await SaveAreaInstanceRec(area);
                }

                await DbContext.SaveChangesAsync();

                var flatList = areaInstances.Flatten(a => a.InverseThis2ParentNavigation).ToList();


                var removedNodes = from c in DbContext.AreaInstances
                    where !(from o in flatList select o.ObjId).Contains(c.ObjId)
                    select c;
                var removedAreasList = removedNodes.ToList();
                DbContext.RemoveRange(removedAreasList);


                await DbContext.SaveChangesAsync(true);
                transaction.Commit();
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogError(e, "Could not save data");
                transaction.Rollback();
            }

            return GetInstances();
        }

        private static AreaInstance RecursiveLoad(AreaInstance parent, AutomaticaContext dbContext)
        {
            var loaded = dbContext.AreaInstances
                .Include(a => a.InverseThis2ParentNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.This2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.NeedsThis2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.ProvidesThis2AreayTypeNavigation).SingleOrDefault(a => a.ObjId == parent.ObjId);

            var newChilds = new List<AreaInstance>();
            if (loaded == null)
            {
                return null;
            }

            foreach (var child in loaded.InverseThis2ParentNavigation)
            {
                newChilds.Add(RecursiveLoad(child, dbContext));
            }

            loaded.InverseThis2ParentNavigation = newChilds;
            return loaded;
        }
    }
}
