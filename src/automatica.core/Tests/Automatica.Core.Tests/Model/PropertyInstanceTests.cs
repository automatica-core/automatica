using System;
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

        [Theory]
        [InlineData("1", 1)]
        [InlineData("42", 42)]
        [InlineData("0", 0)]
        [InlineData("-0", 0)]
        [InlineData("-42", -42)]

        [InlineData("42.42", 42.42)]
        [InlineData("0.0", 0)]
        [InlineData("-42.42", -42.42)]
        [InlineData("-0.0", -0)]
        public void TestPropertyNumeric(string input, double value)
        {
            var node = new NodeInstance
            {
                PropertyInstance = new List<PropertyInstance>
                {
                    CreatePropertyInstance("test", input, PropertyTemplateType.Numeric)
                }
            };

            Assert.Equal(Convert.ToInt32(value), node.GetPropertyValueInt("test"));
            Assert.Equal(value, node.GetPropertyValueDouble("test"));
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
