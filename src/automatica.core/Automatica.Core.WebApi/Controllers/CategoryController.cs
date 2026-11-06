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
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {
                    DbContext.CategoryInstances.Remove(DbContext.CategoryInstances.Single(a => a.ObjId == categoryId));
                    await DbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    throw;
                }

            });

            _categoryCache.Remove(categoryId);
        }

        [HttpPut]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<CategoryInstance> AddOrUpdateInstance([FromBody] CategoryInstance instance)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async
                () =>
            {
                await using var transaction = await DbContext.Database.BeginTransactionAsync();
                try
                {

                    var existingArea =
                        await DbContext.CategoryInstances.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == instance.ObjId);

                    // only the category row, not the posted navigations
                    instance.This2CategoryGroupNavigation = null;
                    instance.This2UserGroupNavigation = null;

                    instance.ModifiedAt = DateTimeOffset.Now;
                    if (existingArea == null)
                    {
                        instance.CreatedAt = DateTimeOffset.Now;
                        await DbContext.CategoryInstances.AddAsync(instance);
                    }
                    else
                    {
                        DbContext.CategoryInstances.Update(instance);
                    }


                    await DbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save data");
                    throw;
                }
            });

            // only after the commit, the cache is reloaded from the database
            _categoryCache.Clear();
            return instance;
        }
    }
}
