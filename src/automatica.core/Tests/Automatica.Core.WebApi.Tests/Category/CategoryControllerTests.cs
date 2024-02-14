using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Categories;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Category
{
    public class CategoryControllerTests : BaseControllerTest<CategoryController>
    {
        [Fact]
        public void TestGetCategories()
        {
            var instances = Controller.GetInstances().ToList();

            Assert.NotEmpty(instances);
        }

        [Fact]
        public void TestGetCategoryGroups()
        {
            var templates = Controller.GetTemplates().ToList();
            Assert.NotEmpty(templates);
        }

        [Fact]
        public async Task TestInvalidSave()
        {
            var instances = Controller.GetInstances().ToList();

            var newInstance = new CategoryInstance
            {
                ObjId = Guid.NewGuid(),
                Name = "TestCategory",
                Color = "green",
                IsFavorite = true,
                Rating = 5,
                Icon = "test-icon"
            };
            instances.Add(newInstance);

            // add instance
            await Assert.ThrowsAsync<DbUpdateException>(async () => await Controller.SaveInstances(instances));
        }

        [Fact]
        public async Task TestAddUpdateRemoveCategoryInstance()
        {
            var templates = Controller.GetTemplates().ToList();
            var instances = Controller.GetInstances().ToList();

            var template = templates.First();
            var newInstance = new CategoryInstance
            {
                ObjId =  Guid.NewGuid(),
                Name = "TestCategory",
                This2CategoryGroup = template.ObjId,
                Color = "green",
                IsFavorite = true,
                Rating = 5,
                Icon = "test-icon"
            };
            instances.Add(newInstance);

            // add instance
            var saved = (await Controller.SaveInstances(instances)).ToList();

            var savedInstance = saved.FirstOrDefault(a => a.ObjId == newInstance.ObjId);
            Assert.NotNull(savedInstance);
            Assert.Equal(newInstance.Name, savedInstance.Name);
            Assert.Equal(newInstance.This2CategoryGroup, savedInstance.This2CategoryGroup);
            Assert.Equal(newInstance.Color, savedInstance.Color);
            Assert.Equal(newInstance.IsFavorite, savedInstance.IsFavorite);
            Assert.Equal(newInstance.Rating, savedInstance.Rating);
            Assert.Equal(newInstance.Icon, savedInstance.Icon);


            // update instance
            savedInstance.Name = "update name...";
            saved = (await Controller.SaveInstances(saved)).ToList();

            var updatedInstance = saved.FirstOrDefault(a => a.ObjId == newInstance.ObjId);
            Assert.NotNull(updatedInstance);
            Assert.Equal(savedInstance.Name, updatedInstance.Name);
            Assert.Equal(savedInstance.This2CategoryGroup, updatedInstance.This2CategoryGroup);
            Assert.Equal(savedInstance.Color, updatedInstance.Color);
            Assert.Equal(savedInstance.IsFavorite, updatedInstance.IsFavorite);
            Assert.Equal(savedInstance.Rating, updatedInstance.Rating);
            Assert.Equal(savedInstance.Icon, updatedInstance.Icon);


            //remove instance
            instances.Remove(newInstance);
            await Controller.RemoveInstance(newInstance.ObjId);
            saved = Controller.GetAllInstances().ToList();

            Assert.Null(saved.FirstOrDefault(a => a.ObjId == newInstance.ObjId));
        }

        [Fact]
        public void TestGenerateIfNotExists()
        {
            var categoryGroupId = Guid.NewGuid();
            using (var dbContext = new AutomaticaContext(Configuration))
            {
                CategoryGroup.GenerateIfNotExists(dbContext, categoryGroupId, "test");

                var categoryGroupCache = ServiceProvider.GetRequiredService<ICategoryGroupCache>();
                categoryGroupCache.Clear();

                dbContext.SaveChanges();
            }

            Assert.NotNull(Controller.GetTemplates().FirstOrDefault(a => a.ObjId == categoryGroupId));
        }
    }
}
