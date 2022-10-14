using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Exceptions;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Moq;
using Xunit;

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

       
        [Fact, TestOrder(7)]
        public void TestGetInstanceData()
        {
            Assert.Throws<RuleNotFoundException>(() => Controller.GetInstanceData(Guid.NewGuid()));
        }


        private void AddLogicTemplate()
        {
            using var db = new AutomaticaContext(Configuration);

            var logicTemplateFactory = new RuleTemplateFactory(db, Configuration, new Mock<IRuleFactory>().Object);

            var factory = new TestLogicFactory();
            factory.InitTemplates(logicTemplateFactory);
        }
    }
}
