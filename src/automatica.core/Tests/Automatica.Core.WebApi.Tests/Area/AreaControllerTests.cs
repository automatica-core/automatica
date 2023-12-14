using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
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

            Controller.DbContext.ChangeTracker.Clear();

            var rootInstance = instances.First();
            var template = templates.First();

            var newInstance = new AreaInstance
            {
                ObjId = Guid.NewGuid(),
                Name = "TestInstance",
                Description = "testDesc",
                This2AreaTemplate = template.ObjId,
                Icon = template.Icon,
                Rating = 10,
                IsFavorite = false,
                This2Parent = rootInstance.ObjId
            };
            rootInstance.InverseThis2ParentNavigation.Add(newInstance);
            
            var saved = (await Controller.SaveInstances(instances)).ToList();

            Assert.NotNull(saved);
            Assert.NotEmpty(saved);
            Assert.Equal(newInstance.Name, saved.First().InverseThis2ParentNavigation.First().Name);
            Assert.Equal(newInstance.Description, saved.First().InverseThis2ParentNavigation.First().Description);
            Assert.Equal(newInstance.ObjId, saved.First().InverseThis2ParentNavigation.First().ObjId);
            Assert.Equal(newInstance.Icon, saved.First().InverseThis2ParentNavigation.First().Icon);
            Assert.Equal(newInstance.Rating, saved.First().InverseThis2ParentNavigation.First().Rating);
            Assert.Equal(newInstance.IsFavorite, saved.First().InverseThis2ParentNavigation.First().IsFavorite);
            Assert.Equal(newInstance.This2Parent, rootInstance.ObjId);

            Assert.Equal(template.ObjId, saved.First().InverseThis2ParentNavigation.First().This2AreaTemplate);

            saved = (await Controller.SaveInstances(instances)).ToList();

            Assert.Empty(saved.First().InverseThis2ParentNavigation);
        }

        [Fact, TestOrder(2)]
        public async Task AddAreaInstances()
        {
            var templates = Controller.GetTemplates().ToList();
            var instances = Controller.GetInstances().ToList();

            var rootInstance = instances.First();
            var template = templates.First();

            var newInstance = new AreaInstance
            {
                ObjId = Guid.NewGuid(),
                This2Parent =  rootInstance.ObjId,
                Name = "TestInstance",
                Description = "testDesc",
                This2AreaTemplate = template.ObjId,
                Icon = template.Icon
            };

            var newInstances = new List<AreaInstance> {newInstance};

            var saved = (await Controller.AddAreaInstances(newInstances)).ToList();

            Assert.NotNull(saved);
            Assert.NotEmpty(saved);
            Assert.Equal(newInstance.Name, saved.First().InverseThis2ParentNavigation.First().Name);
            Assert.Equal(newInstance.ObjId, saved.First().InverseThis2ParentNavigation.First().ObjId);
        }

        [Fact, TestOrder(3)]
        public async Task TestEtsImport()
        {
            var formFileMoq = new Mock<IFormFile>();
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Area", "ETS5_Simple_ThreeLevel.knxproj"), Path.Combine(ServerInfo.GetTempPath(), "ETS5_Simple_ThreeLevel.knxproj"), true);

            formFileMoq.Setup(a => a.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Callback(async (Stream s, CancellationToken cts) =>
            {
                await using var proj = File.Open(Path.Combine(ServerInfo.GetTempPath(), "ETS5_Simple_ThreeLevel.knxproj"), FileMode.Open);
                await proj.CopyToAsync(s, cts);
            });
            formFileMoq.SetupGet(a => a.FileName).Returns("ETS5_Simple_ThreeLevel1.knxproj");

            var structure = (await Controller.ProcessFile(Controller.GetInstances().First(), "", formFileMoq.Object)).ToList();

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


        [Fact, TestOrder(4)]
        public async Task TestEtsImport2()
        {
            var formFileMoq = new Mock<IFormFile>();
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Area", "ETS6_Project.knxproj"), Path.Combine(ServerInfo.GetTempPath(), "ETS6_Project.knxproj"), true);

            formFileMoq.Setup(a => a.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Callback(async (Stream s, CancellationToken cts) =>
            {
                await using var proj = File.Open(Path.Combine(ServerInfo.GetTempPath(), "ETS6_Project.knxproj"), FileMode.Open);
                await proj.CopyToAsync(s, cts);
            });

            formFileMoq.SetupGet(a => a.FileName).Returns("ETS6_Project1.knxproj");

            var structure = (await Controller.ProcessFile(Controller.GetInstances().First(), "", formFileMoq.Object)).ToList();

            Assert.NotNull(structure);
            Assert.NotEmpty(structure);

            Assert.Equal("home", structure.First().Name);

            var demoStructure = structure.First().InverseThis2ParentNavigation.First();
            Assert.Equal("Haus", demoStructure.Name);

            var eg = demoStructure.InverseThis2ParentNavigation.FirstOrDefault(a => a.Name == "EG");
            Assert.NotNull(eg);
            Assert.Equal(12, eg.InverseThis2ParentNavigation.Count);

            var wc = eg.InverseThis2ParentNavigation.FirstOrDefault(a => a.Name == "WC");
            var child1 = eg.InverseThis2ParentNavigation.FirstOrDefault(a => a.Name == "Kind 1");

            Assert.NotNull(wc);
            Assert.NotNull(child1);
        }
    }
}
