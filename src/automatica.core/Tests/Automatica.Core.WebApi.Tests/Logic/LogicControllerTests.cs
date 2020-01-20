using System.Linq;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Logic
{
    public class LogicControllerTests : BaseControllerTest<RulesController>
    {
        [Fact, TestPriority(0)]
        public void TestReadPages()
        {
            var pages = Controller.GetPages().ToList();

            Assert.Collection(pages, page =>
            {
                Assert.Equal("Page1", page.Name);
            });
        }

        [Fact, TestPriority(1)]
        public void TestReadSinglePages()
        {
            var pages = Controller.GetPages().ToList();

            Assert.Collection(pages, page =>
            {
                Assert.Equal("Page1", page.Name);
            });
        }
    }
}
