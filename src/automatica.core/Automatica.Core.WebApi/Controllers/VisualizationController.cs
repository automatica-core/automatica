using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
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
        public VisualizationController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpGet]
        [Route("templates")]
        [Authorize(Policy = Role.ViewerRole)]
        public IList<VisuObjectTemplate> GetTemplates()
        {
            var x = DbContext.VisuObjectTemplates
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.Constraints).ThenInclude(c => c.ConstraintData);
                
            return x.ToList();
        }

        [HttpGet]
        [Route("pages")]
        [Authorize(Policy = Role.ViewerRole)]
        public IList<VisuPage> GetPages()
        {
            var pages = DbContext.VisuPages;

            var ret = new List<VisuPage>();
            foreach (var page in pages)
            {
                ret.Add(LoadPage(page.ObjId));
            }

            return ret;
        }

        [HttpGet]
        [Route("page/default/{pageType}")]
        [Authorize(Policy = Role.ViewerRole)]
        public object LoadDefaultPage(long pageType)
        {
            var defaultPages = DbContext.VisuPages.Where(a => a.This2VisuPageType == pageType && a.DefaultPage).Where(a => IsUserInGroup(a.This2UserGroup)).ToList();

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
            var nodeInstances = DbContext.NodeInstances
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

            var ruleInstances = DbContext.RuleInstances
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

            var nodeInstances = DbContext.NodeInstances
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

            var ruleInstances = DbContext.RuleInstances
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
            var isArea = DbContext.AreaInstances.SingleOrDefault(a => a.ObjId == pageId) != null;
            if(isArea)
            {
                return GetAllByArea(pageId);
            }

            var isCategory = DbContext.CategoryInstances.SingleOrDefault(a => a.ObjId == pageId) != null;

            if(isCategory)
            {
                return GetAllByCategory(pageId);
            }

            var x = LoadPage(pageId);


            return x;
        }

        private VisuPage LoadPage(Guid pageId)
        {
            return DbContext.VisuPages
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(c => c.This2PropertyTemplateNavigation)
                .ThenInclude(c => c.Constraints)
                .ThenInclude(c => c.ConstraintData).Where(a => IsUserInGroup(a.This2UserGroup))
                .Include(a => a.VisuObjectInstances).
                ThenInclude(a => a.This2VisuObjectTemplateNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(c => c.This2PropertyTemplateNavigation).ThenInclude(d => d.This2PropertyTypeNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(b => b.ValueNodeInstanceNavigation)
                .Include(a => a.This2VisuPageTypeNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(b => b.ValueRulePageNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(b => b.ValueVisuPageNavigation)
                .Include(a => a.VisuObjectInstances).ThenInclude(a => a.PropertyInstance).ThenInclude(a => a.ValueAreaInstanceNavigation)
                .SingleOrDefault(a => a.ObjId == pageId);
            ;
        }

        [HttpPost]
        [Route("pages")]
        [Authorize(Policy = Role.AdminRole)]
        public IList<VisuPage> SavePages([FromBody] IList<VisuPage> pages)
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
