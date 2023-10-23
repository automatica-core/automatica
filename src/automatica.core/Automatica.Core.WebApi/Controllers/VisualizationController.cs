using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
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

        public VisualizationController(ILogger<VisualizationController> logger, AutomaticaContext dbContext, IVisualizationCache cache) : base(dbContext)
        {
            _logger = logger;
            _cache = cache;
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
            return _cache.PageCache.ByFavorites();
        }

        [HttpGet]
        [Route("categoryLinked/{areaInstance}")]
        [Authorize(Policy = Role.ViewerRole)]
        public VisualizationDataFacade GetAllByCategory(Guid categoryInstance)
        {
            return _cache.PageCache.AllByCategory(categoryInstance);
        }

        [HttpGet]
        [Route("areaLinked/{areaInstance}")]
        [Authorize(Policy = Role.ViewerRole)]
        public VisualizationDataFacade GetAllByArea(Guid areaInstance)
        {
            return _cache.PageCache.AllByArea(areaInstance);
        }

        [HttpGet]
        [Route("page/{pageId}")]
        [Authorize(Policy = Role.ViewerRole)]
        public object GetPage(Guid pageId)
        {
            return _cache.PageCache.ByPage(pageId);
        }

    }
}
