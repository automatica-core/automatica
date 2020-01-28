using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models.Categories;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.EntityFrameworkCore;
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


            // update instance
            savedInstance.Name = "update name...";
            saved = (await Controller.SaveInstances(instances)).ToList();

            savedInstance = saved.FirstOrDefault(a => a.ObjId == newInstance.ObjId);
            Assert.NotNull(savedInstance);
            Assert.Equal(savedInstance.Name, savedInstance.Name);


            //remove instance
            instances.Remove(newInstance);
            saved = (await Controller.SaveInstances(instances)).ToList();

            Assert.Null(saved.FirstOrDefault(a => a.ObjId == newInstance.ObjId));
        }
    }
}
