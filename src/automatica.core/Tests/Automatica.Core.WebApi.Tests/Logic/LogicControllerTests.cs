using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Exceptions;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Moq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Logic
{
    public class LogicControllerTests : BaseControllerTest<LogicsController>
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

            var sPage = Controller.GetPage(page.ObjId);

            Assert.Equal(page.ObjId, sPage.ObjId);
        }

        [Fact, TestOrder(2)]
        public void TestGetRuleTemplates()
        {
            var templates = Controller.GetLogicTemplates();

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

            var logicTemplateFactory = new LogicTemplateFactory(db, Configuration, new Mock<ILogicFactory>().Object);

            var factory = new TestLogicFactory();
            factory.InitTemplates(logicTemplateFactory);
        }
    }
}
