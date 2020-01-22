using System.IO;
using System.Threading.Tasks;
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
        public async Task TestEtsImport()
        {
            var formFileMoq = new Mock<IFormFile>();

            formFileMoq.Setup(a => a.CopyTo(It.IsAny<Stream>())).Callback((Stream s) =>
            {
                // impl copy to
            });

            await Controller.ProcessFile(null, formFileMoq.Object);
        }
    }
}
