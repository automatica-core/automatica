using System.Collections.Generic;
using Automatica.Core.EF.Exceptions;
using Automatica.Core.EF.Models;
using Xunit;

namespace Automatica.Core.Tests.Model
{
    public class PropertyInstanceTests
    {
        [Fact]
        public void TestPropertyNotFound()
        {
            var node = new NodeInstance
            {

            };

            Assert.Equal(nameof(NodeInstance), node.TypeInfo);
            Assert.Throws<PropertyNotFoundException>(() => node.GetProperty("asdf"));
        }

        private PropertyInstance CreatePropertyInstance(string key, object value, PropertyTemplateType type)
        {
            return new PropertyInstance
            {
                This2PropertyTemplateNavigation = new PropertyTemplate
                {
                    Key = key,
                    This2PropertyTypeNavigation = new PropertyType
                    {
                        Type = (long)type
                    }
                },
                Value = value
            };
        }

        [Fact]
        public void TestPropertyText()
        {
            var node = new NodeInstance
            {
                PropertyInstance = new List<PropertyInstance>
                {
                    CreatePropertyInstance("test", "stringValue", PropertyTemplateType.Text)
                }
            };

            var strValue = node.GetPropertyValueString("test");
            Assert.Equal("stringValue", strValue);
        }

        [Fact]
        public void TestPropertyNumeric()
        {
            var node = new NodeInstance
            {
                PropertyInstance = new List<PropertyInstance>
                {
                    CreatePropertyInstance("test", 123, PropertyTemplateType.Numeric)
                }
            };

            Assert.Equal(123, node.GetPropertyValueInt("test"));
            Assert.Equal(123, node.GetPropertyValueDouble("test"));
        }
        [Fact]
        public void TestPropertyBoolean()
        {
            var node = new NodeInstance
            {
                PropertyInstance = new List<PropertyInstance>
                {
                    CreatePropertyInstance("test", false, PropertyTemplateType.Bool)
                }
            };
            var prop = node.GetProperty("test");
            Assert.NotNull(prop.ValueBool);
            Assert.False(prop.ValueBool.Value);
        }
    }
}
