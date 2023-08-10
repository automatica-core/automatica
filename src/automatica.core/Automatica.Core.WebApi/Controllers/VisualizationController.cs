using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
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


        [HttpPost]
        [Route("pages")]
        [Authorize(Policy = Role.AdminRole)]
        public ICollection<VisuPage> SavePages([FromBody] IList<VisuPage> pages)
        {
            var transaction = DbContext.Database.BeginTransaction();
            try
            {
                foreach (var page in pages)
                {
                    if (page.Description == null)
                    {
                        page.Description = "";
                    }

                    bool isNewPage = DbContext.VisuPages.AsNoTracking().SingleOrDefault(a => a.ObjId == page.ObjId) ==
                                     null;
                    foreach (var visuObject in page.VisuObjectInstances)
                    {
                        if (visuObject.Description == null)
                        {
                            visuObject.Description = "";
                        }

                        bool isNewVisuObject = DbContext.VisuObjectInstances.AsNoTracking()
                                                   .SingleOrDefault(a => a.ObjId == visuObject.ObjId) == null;

                        visuObject.This2VisuObjectTemplateNavigation = null;
                        foreach (var property in visuObject.PropertyInstance)
                        {
                            bool isNewProperty = DbContext.PropertyInstances.AsNoTracking()
                                                     .SingleOrDefault(a => a.ObjId == property.ObjId) == null;

                            property.ValueVisuPageNavigation = null;
                            property.This2PropertyTemplateNavigation = null;

                            if (isNewProperty)
                            {
                                DbContext.Add(property);
                            }
                            else
                            {
                                DbContext.Update(property);
                            }
                        }

                        if (isNewVisuObject)
                        {
                            DbContext.Add(visuObject);
                        }
                        else
                        {
                            DbContext.Update(visuObject);
                        }
                    }

                    if (isNewPage)
                    {
                        DbContext.VisuPages.Add(page);
                    }
                    else
                    {
                        DbContext.VisuPages.Update(page);
                    }
                }

                var removedPages = from c in DbContext.VisuPages
                    where !(from o in pages select o.ObjId).Contains(c.ObjId)
                    select c;

                DbContext.RemoveRange(removedPages);
                foreach (var page in pages)
                {
                    var removedVisuInstances = from c in DbContext.VisuObjectInstances
                        where !(from o in page.VisuObjectInstances select o.ObjId).Contains(c.ObjId) &&
                              c.This2VisuPage == page.ObjId
                        select c;

                    var removedVisuInstancesList = removedVisuInstances.ToList();
                    DbContext.RemoveRange(removedVisuInstancesList);
                }


                DbContext.SaveChanges();
                transaction.Commit();

                _cache.ClearInstances();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                _logger.LogError(e, "Could not save visu", e);
            }

            return GetPages();
        }

    }
}
