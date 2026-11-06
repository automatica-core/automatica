using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Visualization;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            return _cache.PageCache.All().Where(a => IsUserInGroup(a.This2UserGroup)).ToList();
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

    }
}
