using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Visualization;
using Automatica.Core.Model;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    public class AutomaticVisualizationData : TypedObject
    {
        public IList<RuleInstance> RuleInstances { get; set; }
        public IList<NodeInstance> NodeInstances { get; set; }
    }

    [Route("visualization")]
    public class VisualizationController : BaseController
    {
        private readonly IVisualizationCache _cache;

        public VisualizationController(AutomaticaContext dbContext, IVisualizationCache cache) : base(dbContext)
        {
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
            var defaultPages = _cache.PageCache.All().Where(a => a.This2VisuPageType == pageType && a.DefaultPage).Where(a => IsUserInGroup(a.This2UserGroup)).ToList();

            if (defaultPages.Count > 0)
            {
                return GetPage(defaultPages[0].ObjId);
            }

            throw new ArgumentException("could not find a default page");
        }

        [HttpGet]
        [Route("categoryLinked/{areaInstance}")]
        [Authorize(Policy = Role.ViewerRole)]
        public AutomaticVisualizationData GetAllByCategory(Guid categoryInstance)
        {
            var data = new AutomaticVisualizationData();
            var nodeInstances = DbContext.NodeInstances.AsNoTracking()
                     .Include(a => a.PropertyInstance)
                     .Include(a => a.This2NodeTemplateNavigation)
                     .Include(a => a.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation)
                     .Include(a => a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation)
                     .Include(a => a.This2NodeTemplateNavigation.PropertyTemplate)
                     .ThenInclude(b => b.This2PropertyTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a => a.PropertyInstance)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a =>
                         a.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a =>
                         a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate)
                     .ThenInclude(b => b.This2PropertyTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate).ThenInclude(b => b.Constraints)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate).ThenInclude(b => b.Constraints)
                     .ThenInclude(a => a.ConstraintData)
                     .Include(a => a.This2AreaInstanceNavigation)
                     .Include(a => a.This2CategoryInstanceNavigation)
                     .Where(a => a.This2CategoryInstance == categoryInstance && a.UseInVisu).Where(a => IsUserInGroup(a.This2UserGroup));

            data.NodeInstances = nodeInstances.ToList();

            var ruleInstances = DbContext.RuleInstances.AsNoTracking()
                    .Include(a => a.This2RuleTemplateNavigation)
                    .ThenInclude(a => a.RuleInterfaceTemplate)
                    .Include(a => a.RuleInterfaceInstance)
                    .ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                    .ThenInclude(a => a.This2RuleInterfaceDirectionNavigation)
                    .Include(a => a.This2AreaInstanceNavigation)
                    .Include(a => a.This2CategoryInstanceNavigation)
                    .Where(a => a.This2CategoryInstance == categoryInstance && a.UseInVisu).Where(a => IsUserInGroup(a.This2UserGroup));

            data.RuleInstances = ruleInstances.ToList();
            return data;
        }

        [HttpGet]
        [Route("areaLinked/{areaInstance}")]
        [Authorize(Policy = Role.ViewerRole)]
        public AutomaticVisualizationData GetAllByArea(Guid areaInstance)
        {
            var data = new AutomaticVisualizationData();

            var nodeInstances = DbContext.NodeInstances.AsNoTracking()
                     .Include(a => a.PropertyInstance)
                     .Include(a => a.This2NodeTemplateNavigation)
                     .Include(a => a.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation)
                     .Include(a => a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation)
                     .Include(a => a.This2NodeTemplateNavigation.PropertyTemplate)
                     .ThenInclude(b => b.This2PropertyTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a => a.PropertyInstance)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a =>
                         a.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a =>
                         a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate)
                     .ThenInclude(b => b.This2PropertyTypeNavigation)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate).ThenInclude(b => b.Constraints)
                     .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                     .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate).ThenInclude(b => b.Constraints)
                     .ThenInclude(a => a.ConstraintData)
                     .Include(a => a.This2AreaInstanceNavigation)
                     .Include(a => a.This2CategoryInstanceNavigation)
                     .Where(a => a.This2AreaInstance == areaInstance && a.UseInVisu).Where(a => IsUserInGroup(a.This2UserGroup));

            data.NodeInstances = nodeInstances.ToList();

            var ruleInstances = DbContext.RuleInstances.AsNoTracking()
                    .Include(a => a.This2RuleTemplateNavigation)
                    .ThenInclude(a => a.RuleInterfaceTemplate)
                    .Include(a => a.RuleInterfaceInstance)
                    .ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                    .ThenInclude(a => a.This2RuleInterfaceDirectionNavigation)
                    .Include(a => a.This2AreaInstanceNavigation)
                    .Include(a => a.This2CategoryInstanceNavigation)
                    .Where(a => a.This2AreaInstance == areaInstance && a.UseInVisu).Where(a => IsUserInGroup(a.This2UserGroup));

            data.RuleInstances = ruleInstances.ToList();
            return data;
        }

        [HttpGet]
        [Route("page/{pageId}")]
        [Authorize(Policy = Role.ViewerRole)]
        public object GetPage(Guid pageId)
        {
            var isArea = DbContext.AreaInstances.AsNoTracking().SingleOrDefault(a => a.ObjId == pageId) != null;
            if(isArea)
            {
                return GetAllByArea(pageId);
            }

            var isCategory = DbContext.CategoryInstances.AsNoTracking().SingleOrDefault(a => a.ObjId == pageId) != null;

            if(isCategory)
            {
                return GetAllByCategory(pageId);
            }

            var x = _cache.PageCache.Get(pageId);

            return x;
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
                SystemLogger.Instance.LogError(e, "Could not save visu", e);
            }

            return GetPages();
        }

    }
}
