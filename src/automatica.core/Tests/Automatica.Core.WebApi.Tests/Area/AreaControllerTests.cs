using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Area
{
    public class AreaControllerTests : BaseControllerTest<AreaController>
    {
        [Fact, TestOrder(0)]
        public void GetAreaTemplates()
        {
            var templates = Controller.GetTemplates();
            Assert.NotEmpty(templates);
        }

        [Fact, TestOrder(1)]
        public void GetAreaInstances()
        {
            var instances = Controller.GetInstances();
            Assert.NotEmpty(instances);
        }

        [Fact, TestOrder(2)]
        public async Task SaveAreaInstances()
        {
            var templates = Controller.GetTemplates().ToList();
            var instances = Controller.GetInstances().ToList();

            var rootInstance = instances.First();
            var template = templates.First();

            var newInstance = new AreaInstance
            {
                ObjId = Guid.NewGuid(),
                Name = "TestInstance",
                Description = "testDesc",
                This2AreaTemplate = template.ObjId,
                Icon = template.Icon
            };
            rootInstance.InverseThis2ParentNavigation.Add(newInstance);

            var saved = (await Controller.SaveInstances(instances)).ToList();

            Assert.NotNull(saved);
            Assert.NotEmpty(saved);
            Assert.Equal(newInstance.Name, saved.First().InverseThis2ParentNavigation.First().Name);
            Assert.Equal(newInstance.ObjId, saved.First().InverseThis2ParentNavigation.First().ObjId);
        }

        [Fact, TestOrder(3)]
        public async Task TestEtsImport()
        {
            var formFileMoq = new Mock<IFormFile>();

            formFileMoq.Setup(a => a.CopyTo(It.IsAny<Stream>())).Callback((Stream s) =>
            {
                using var proj = File.Open(Path.Combine("Area", "ETS5_Simple_ThreeLevel.knxproj"), FileMode.Open);
                proj.CopyTo(s);
            });
            formFileMoq.SetupGet(a => a.FileName).Returns("Upload.knxproj");

            var structure = (await Controller.ProcessFile(Controller.GetInstances().First(), formFileMoq.Object)).ToList();

            Assert.NotNull(structure);
            Assert.NotEmpty(structure);

            Assert.Equal("Project", structure.First().Name);

            var demoStructure = structure.First().InverseThis2ParentNavigation.First();
            Assert.Equal("DemoStructure", demoStructure.Name);

            var building1 = demoStructure.InverseThis2ParentNavigation.FirstOrDefault(a => a.Name == "Building 1");
            Assert.NotNull(building1);
            Assert.Equal(2, building1.InverseThis2ParentNavigation.Count);

            var firstFloor = building1.InverseThis2ParentNavigation.FirstOrDefault(a => a.Name == "First");
            var groundFloor = building1.InverseThis2ParentNavigation.FirstOrDefault(a => a.Name == "Ground");

            Assert.NotNull(firstFloor);
            Assert.NotNull(groundFloor);

            Assert.Equal("Sleep", firstFloor.InverseThis2ParentNavigation.First().Name);
            Assert.Equal("Living", groundFloor.InverseThis2ParentNavigation.First().Name);

            var garage = demoStructure.InverseThis2ParentNavigation.FirstOrDefault(a => a.Name == "Garage");
            Assert.NotNull(garage);
            Assert.Empty(garage.InverseThis2ParentNavigation);
        }
    }
}
