using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Visualization;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/visualization")]
    public class VisualizationController : BaseController
    {
        private readonly ILogger<VisualizationController> _logger;
        private readonly IVisualizationCache _cache;
        private readonly IAreaCache _areaCache;
        private readonly ICategoryCache _categoryCache;

        public VisualizationController(ILogger<VisualizationController> logger, AutomaticaContext dbContext, IVisualizationCache cache, IAreaCache areaCache, ICategoryCache categoryCache) : base(dbContext)
        {
            _logger = logger;
            _cache = cache;
            _areaCache = areaCache;
            _categoryCache = categoryCache;
        }

        /// <summary>
        /// Only the nodes and logics of the facade the user may see (a new object, the cached one is shared).
        /// </summary>
        private VisualizationDataFacade Filter(VisualizationDataFacade facade)
        {
            return new VisualizationDataFacade
            {
                NodeInstances = facade.NodeInstances?.Where(a => CanAccess(a.This2UserGroup)).ToList(),
                RuleInstances = facade.RuleInstances?.Where(a => CanAccess(a.This2UserGroup)).ToList()
            };
        }

        [HttpGet]
        [Route("templates")]
        [Authorize(Policy = Role.ViewerRole)]
        public ICollection<VisuObjectTemplate> GetTemplates()
        {
            return _cache.TemplateCache.All();
        }

        [HttpGet]
        [Route("pages")]
        [Authorize(Policy = Role.ViewerRole)]
        public ICollection<VisuPage> GetPages()
        {
            return _cache.PageCache.All().Where(a => CanAccess(a.This2UserGroup)).ToList();
        }

        [HttpGet]
        [Route("page/default/{pageType}")]
        [Authorize(Policy = Role.ViewerRole)]
        public object LoadDefaultPage(long pageType)
        {
            return Filter(_cache.PageCache.ByFavorites());
        }

        [HttpGet]
        [Route("categoryLinked/{categoryInstance}")]
        [Authorize(Policy = Role.ViewerRole)]
        public ActionResult<VisualizationDataFacade> GetAllByCategory(Guid categoryInstance)
        {
            if (!CanAccess(_categoryCache.Get(categoryInstance)?.This2UserGroup))
            {
                return NotFound();
            }
            return Filter(_cache.PageCache.AllByCategory(categoryInstance));
        }

        [HttpGet]
        [Route("areaLinked/{areaInstance}")]
        [Authorize(Policy = Role.ViewerRole)]
        public ActionResult<VisualizationDataFacade> GetAllByArea(Guid areaInstance)
        {
            if (!CanAccess(_areaCache.Get(areaInstance)?.This2UserGroup))
            {
                return NotFound();
            }
            return Filter(_cache.PageCache.AllByArea(areaInstance));
        }

        [HttpGet]
        [Route("page/{pageId}")]
        [Authorize(Policy = Role.ViewerRole)]
        public IActionResult GetPage(Guid pageId)
        {
            var page = _cache.PageCache.ByPage(pageId);

            switch (page)
            {
                case null:
                    return NotFound();
                case VisualizationDataFacade facade:
                    // an area or a category
                    var group = _areaCache.Get(pageId)?.This2UserGroup ?? _categoryCache.Get(pageId)?.This2UserGroup;
                    if (!CanAccess(group))
                    {
                        return NotFound();
                    }
                    return Ok(Filter(facade));
                case VisuPage visuPage when !CanAccess(visuPage.This2UserGroup):
                    return NotFound();
                default:
                    return Ok(page);
            }
        }


        /// <summary>
        /// Saves the pages of the visu editor: the posted pages with their objects and properties are added or updated,
        /// the pages (of the ones the user may access) and the objects of a posted page that are not posted any more
        /// are deleted. Only visu properties are written, a property of a node instance can not be changed here.
        /// </summary>
        [HttpPost]
        [Route("pages")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<ActionResult<ICollection<VisuPage>>> SavePages([FromBody] IList<VisuPage> pages)
        {
            if (pages == null || pages.Any(a => a == null))
            {
                return BadRequest();
            }

            var objects = pages.SelectMany(a => a.VisuObjectInstances ?? new List<VisuObjectInstance>()).ToList();
            var properties = objects.Where(a => a != null).SelectMany(a => a.PropertyInstance ?? new List<PropertyInstance>()).ToList();
            if (objects.Any(a => a == null) || properties.Any(a => a == null)
                || HasDuplicates(pages.Select(a => a.ObjId))
                || HasDuplicates(objects.Select(a => a.ObjId))
                || HasDuplicates(properties.Select(a => a.ObjId)))
            {
                return BadRequest();
            }

            var propertyIds = properties.Select(a => a.ObjId).ToList();
            var existingProperties = await DbContext.PropertyInstances.AsNoTracking()
                .Where(a => propertyIds.Contains(a.ObjId))
                .Select(a => new { a.ObjId, a.This2VisuObjectInstance })
                .ToListAsync();
            if (existingProperties.Any(a => a.This2VisuObjectInstance == null))
            {
                // the property of a node instance
                return BadRequest();
            }

            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                DbContext.ChangeTracker.Clear();
                await using var transaction = await DbContext.Database.BeginTransactionAsync();

                var existingPages = await DbContext.VisuPages.AsNoTracking().Select(a => new { a.ObjId, a.This2UserGroup }).ToListAsync();
                var existingPageIds = existingPages.Select(a => a.ObjId).ToHashSet();
                var objectIds = objects.Select(a => a.ObjId).ToList();
                var existingObjectIds = (await DbContext.VisuObjectInstances.AsNoTracking()
                    .Where(a => objectIds.Contains(a.ObjId)).Select(a => a.ObjId).ToListAsync()).ToHashSet();
                var existingPropertyIds = existingProperties.Select(a => a.ObjId).ToHashSet();

                foreach (var page in pages)
                {
                    var pageObjects = page.VisuObjectInstances ?? new List<VisuObjectInstance>();
                    page.VisuObjectInstances = null;
                    page.This2UserGroupNavigation = null;
                    page.This2VisuPageTypeNavigation = null;
                    page.Description ??= "";
                    page.Name ??= "";
                    Upsert(page, existingPageIds.Contains(page.ObjId));

                    foreach (var visuObject in pageObjects)
                    {
                        var objectProperties = visuObject.PropertyInstance ?? new List<PropertyInstance>();
                        visuObject.PropertyInstance = null;
                        visuObject.This2VisuPage = page.ObjId;
                        visuObject.This2VisuPageNavigation = null;
                        visuObject.This2VisuObjectTemplateNavigation = null;
                        visuObject.This2UserGroupNavigation = null;
                        visuObject.Description ??= "";
                        visuObject.Name ??= "";
                        Upsert(visuObject, existingObjectIds.Contains(visuObject.ObjId));

                        foreach (var property in objectProperties)
                        {
                            property.This2VisuObjectInstance = visuObject.ObjId;
                            property.This2NodeInstance = null;
                            property.This2NodeInstanceNavigation = null;
                            property.This2VisuObjectInstanceNavigation = null;
                            property.This2PropertyTemplateNavigation = null;
                            property.ValueNodeInstanceNavigation = null;
                            property.ValueRulePageNavigation = null;
                            property.ValueVisuPageNavigation = null;
                            property.ValueAreaInstanceNavigation = null;
                            property.ValueCategoryInstanceNavigation = null;
                            property.ValueSlaveNavigation = null;
                            Upsert(property, existingPropertyIds.Contains(property.ObjId));
                        }
                    }
                }
                await DbContext.SaveChangesAsync();

                var postedPageIds = pages.Select(a => a.ObjId).ToHashSet();
                var removedPageIds = existingPages
                    .Where(a => !postedPageIds.Contains(a.ObjId) && CanAccess(a.This2UserGroup))
                    .Select(a => a.ObjId)
                    .ToList();
                var postedObjectIds = objectIds.ToHashSet();
                var removedObjects = (await DbContext.VisuObjectInstances
                        .Where(a => postedPageIds.Contains(a.This2VisuPage) || removedPageIds.Contains(a.This2VisuPage))
                        .ToListAsync())
                    .Where(a => !postedObjectIds.Contains(a.ObjId))
                    .ToList();
                var removedObjectIds = removedObjects.Select(a => (Guid?)a.ObjId).ToList();

                DbContext.PropertyInstances.RemoveRange(await DbContext.PropertyInstances
                    .Where(a => removedObjectIds.Contains(a.This2VisuObjectInstance))
                    .ToListAsync());
                DbContext.VisuObjectInstances.RemoveRange(removedObjects);
                DbContext.VisuPages.RemoveRange(await DbContext.VisuPages.Where(a => removedPageIds.Contains(a.ObjId)).ToListAsync());

                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            });

            DbContext.ChangeTracker.Clear();
            _cache.ClearInstances();
            _logger.LogInformation("Saved {Count} visu pages", pages.Count);

            return new ActionResult<ICollection<VisuPage>>(GetPages());
        }

        private void Upsert<T>(T entity, bool exists) where T : class
        {
            if (exists)
            {
                DbContext.Update(entity);
            }
            else
            {
                DbContext.Add(entity);
            }
        }

        private static bool HasDuplicates(IEnumerable<Guid> ids)
        {
            var seen = new HashSet<Guid>();
            return ids.Any(id => !seen.Add(id));
        }
    }
}
