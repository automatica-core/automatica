using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Runtime.Exceptions;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace Automatica.Core.WebApi.Tests.Logic
{
    public class LogicControllerTests : BaseControllerTest<RulesController>
    {
        public LogicControllerTests()
        {
            AddLogicTemplate();
        }

        [Fact, TestOrder(0)]
        public void TestReadPages()
        {
            var pages = Controller.GetPages().ToList();

            Assert.NotEmpty(pages);
            Assert.Collection(pages, page =>
            {
                Assert.Equal("Page1", page.Name);
            });
        }

        [Fact, TestOrder(1)]
        public void TestReadSinglePages()
        {
            var pages = Controller.GetPages().ToList();
            var page = pages.First();

            var sPage = Controller.GetRulePage(page.ObjId);

            Assert.Equal(page.ObjId, sPage.ObjId);
        }

        [Fact, TestOrder(2)]
        public void TestGetRuleTemplates()
        {
            var templates = Controller.GetRuleTemplates();

            Assert.NotEmpty(templates);
        }

        [Fact, TestOrder(3)]
        public async Task TestRenamePages()
        {
            var pages = Controller.GetPages().ToList();

            var page = pages.First();
            page.Name = "TestPage1";

            var savedPages = await Controller.SaveAll(pages);

            Assert.Equal(page.Name, savedPages.First().Name);
        }


        [Fact, TestOrder(4)]
        public async Task TestAddAndUpdatePages()
        {
            var pages = Controller.GetPages().ToList();

            var page = new RulePage
            {
                ObjId = Guid.NewGuid(),
                Name = "NewPage1",
                Description = "NewPage1Description",
                This2RulePageType = 1
            };
            pages.Add(page);

            var savedPages = (await Controller.SaveAll(pages)).ToList();

            Assert.Equal(page.Name, savedPages.First(a => a.ObjId == page.ObjId).Name);

            page.Name = "UpdatePage1";
            page.Description = null;
            var savedPages2 = (await Controller.SaveAll(pages)).ToList();

            Assert.Equal(page.Name, savedPages2.First(a => a.ObjId == page.ObjId).Name);
        }

        [Fact, TestOrder(5)]
        public async Task TestAddLogicInstance()
        {
            using var db = new AutomaticaContext(Configuration);

            var logicTemplateFactory = new RuleTemplateFactory(db, Configuration);

            var pages = Controller.GetPages().ToList();
            var page = pages.First();

            var ruleInstance = logicTemplateFactory.CreateRuleInstance(Controller.GetRuleTemplates().First());
            Assert.Equal(2, ruleInstance.RuleInterfaceInstance.Count);
            Assert.NotEmpty(ruleInstance.RuleInterfaceInstance.Where(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)RuleInterfaceDirection.Input));
            Assert.NotEmpty(ruleInstance.RuleInterfaceInstance.Where(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)RuleInterfaceDirection.Output));

            page.RuleInstance.Add(ruleInstance);

            var savedPages = await Controller.SaveAll(pages);

            Assert.Equal(savedPages.First().RuleInstance.First().ObjId, ruleInstance.ObjId);
        }

        [Fact, TestOrder(6)]
        public async Task TestAddLogicInstancesAndLink()
        {
            await using var db = new AutomaticaContext(Configuration);

            var logicTemplateFactory = new RuleTemplateFactory(db, Configuration);

            var pages = Controller.GetPages().ToList();
            var page = pages.First();

            var ruleInstance1 = logicTemplateFactory.CreateRuleInstance(Controller.GetRuleTemplates().First());
            var ruleInstance2 = logicTemplateFactory.CreateRuleInstance(Controller.GetRuleTemplates().First());

            page.RuleInstance.Add(ruleInstance1);
            page.RuleInstance.Add(ruleInstance2);

            page.Link.Add(new Link
            {
                This2RuleInterfaceInstanceInputNavigation = ruleInstance2.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)RuleInterfaceDirection.Input),
                This2RuleInterfaceInstanceOutputNavigation= ruleInstance2.RuleInterfaceInstance.First(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)RuleInterfaceDirection.Output)
            });

            var savedPages = (await Controller.SaveAll(pages)).ToList();

            Assert.Equal(2, savedPages.First().RuleInstance.Count);
            Assert.NotEmpty(savedPages.First().Link);
        }

        [Fact, TestOrder(8)]
        public async Task RemoveLogicInstance()
        {
            await using var db = new AutomaticaContext(Configuration);

            var logicTemplateFactory = new RuleTemplateFactory(db, Configuration);

            var pages = Controller.GetPages().ToList();
            var page = pages.First();

            var ruleInstance1 = logicTemplateFactory.CreateRuleInstance(Controller.GetRuleTemplates().First());
            var ruleInstance2 = logicTemplateFactory.CreateRuleInstance(Controller.GetRuleTemplates().First());

            page.RuleInstance.Add(ruleInstance1);
            page.RuleInstance.Add(ruleInstance2);

            var savedPages = (await Controller.SaveAll(pages)).ToList();

            var ruleInstance2ToRemove = savedPages.First().RuleInstance.First(a => a.ObjId == ruleInstance2.ObjId);
            savedPages.First().RuleInstance.Remove(ruleInstance2ToRemove);

            savedPages = (await Controller.SaveAll(savedPages)).ToList();

            Assert.Single(savedPages.First().RuleInstance);
            Assert.Equal(savedPages.First().RuleInstance.First().ObjId, ruleInstance1.ObjId);
        }

        [Fact, TestOrder(7)]
        public void TestGetInstanceData()
        {
            Assert.Throws<RuleNotFoundException>(() => Controller.GetInstanceData(Guid.NewGuid()));
        }


        [Fact, TestOrder(99)]
        public async Task RemoveAll()
        {
            await using var db = new AutomaticaContext(Configuration);

            var empty = await Controller.SaveAll(new List<RulePage>());

            Assert.Empty(empty);
        }

        private void AddLogicTemplate()
        {
            using var db = new AutomaticaContext(Configuration);

            var logicTemplateFactory = new RuleTemplateFactory(db, Configuration);

            var factory = new TestLogicFactory();
            factory.InitTemplates(logicTemplateFactory);
        }
    }
}
