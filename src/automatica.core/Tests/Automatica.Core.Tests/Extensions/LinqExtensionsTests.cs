using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.LinqExtensions;
using Automatica.Core.EF.Models.Areas;
using Xunit;

namespace Automatica.Core.Tests.Extensions
{
    public class LinqExtensionsTests
    {
        [Fact]
        public void TestFlatten()
        {
            var area = new AreaInstance
            {
                Name = "root"
            };
            var child1 = new AreaInstance
            {
                Name = "child1"
            };
            var child2 = new AreaInstance
            {
                Name = "child2"
            };

            area.InverseThis2ParentNavigation.Add(child1);
            child1.InverseThis2ParentNavigation.Add(child2);


            var list = new List<AreaInstance>();
            list.Add(area);

            var flatten = list.Flatten(a => a.InverseThis2ParentNavigation).ToList();


            Assert.NotNull(flatten);
            Assert.Equal(3, flatten.Count);
        }
    }
}
