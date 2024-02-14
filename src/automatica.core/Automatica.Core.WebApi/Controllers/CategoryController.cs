using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/categories")]
    public class CategoryController : BaseController
    {
        private readonly ICategoryCache _categoryCache;
        private readonly ICategoryGroupCache _categoryGroupCache;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(AutomaticaContext dbContext, ICategoryCache categoryCache,
            ICategoryGroupCache categoryGroupCache, ILogger<CategoryController> logger) : base(dbContext)
        {
            _categoryCache = categoryCache;
            _categoryGroupCache = categoryGroupCache;
            _logger = logger;
        }

        [HttpGet]
        [Route("groups")]
        [Authorize(Policy = Role.ViewerRole)]
        public IEnumerable<CategoryGroup> GetTemplates()
        {
            return _categoryGroupCache.All();
        }

        [HttpGet]
        [Authorize(Policy = Role.ViewerRole)]
        public IEnumerable<CategoryInstance> GetInstances()
        {
            return _categoryCache.All().Where(a => IsUserInGroup(a.This2UserGroup));
        }

        [HttpGet("all")]
        [Authorize(Policy = Role.AdminRole)]
        public IEnumerable<CategoryInstance> GetAllInstances()
        {
            return _categoryCache.All();
        }

        [HttpDelete("{categoryId}")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task RemoveInstance(Guid categoryId)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    var area = _categoryCache.Get(categoryId);
                    
                    await DbContext.SaveChangesAsync();


                    DbContext.Remove(area);
                    _categoryCache.Remove(categoryId);
                    await DbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    await transaction.RollbackAsync();
                    throw;
                }

            });
        }

        [HttpPut]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<CategoryInstance>> AddOrUpdateInstance([FromBody] CategoryInstance instance)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    var existingArea =
                        await DbContext.CategoryInstances.SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);

                    instance.ModifiedAt = DateTimeOffset.Now;
                    if (existingArea == null)
                    {
                        instance.CreatedAt = DateTimeOffset.Now;
                        await DbContext.CategoryInstances.AddAsync(instance);
                        _categoryCache.Add(instance.ObjId, instance);
                    }
                    else
                    {
                        DbContext.Entry(existingArea).State = EntityState.Detached;
                        DbContext.CategoryInstances.Update(instance);
                        _categoryCache.Update(instance.ObjId, instance);
                    }


                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _categoryCache.Clear();
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
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IEnumerable<CategoryInstance>> SaveInstances([FromBody]IList<CategoryInstance> instances)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            return await strategy.Execute(async
                () =>
            {
                var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var instance in instances)
                    {
                        var existingArea =
                            await DbContext.CategoryInstances.SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);

                        instance.ModifiedAt = DateTimeOffset.Now;
                        if (existingArea == null)
                        {
                            instance.CreatedAt = DateTimeOffset.Now;
                            await DbContext.CategoryInstances.AddAsync(instance);
                        }
                        else
                        {
                            DbContext.Entry(existingArea).State = EntityState.Detached;
                            DbContext.CategoryInstances.Update(instance);
                        }
                    }

                    var removedNodes = from c in DbContext.CategoryInstances
                        where !(from o in instances select o.ObjId).Contains(c.ObjId)
                        select c;
                    var removedList = removedNodes.ToList();
                    DbContext.RemoveRange(removedList);

                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _categoryCache.Clear();
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
