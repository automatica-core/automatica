using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Automatica.Core.WebApi.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Authentication
{
    public class NonAdminResponseFilterTests
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private static PropertyInstance Property(PropertyTemplateType type, string value)
        {
            var template = new PropertyTemplate
            {
                ObjId = Guid.NewGuid(),
                This2PropertyType = (long)type,
                This2PropertyTypeNavigation = new PropertyType { Type = (long)type }
            };
            return new PropertyInstance
            {
                ObjId = Guid.NewGuid(),
                This2PropertyTemplate = template.ObjId,
                This2PropertyTemplateNavigation = template,
                ValueString = value
            };
        }

        [Fact]
        public void PasswordValues_AndOtherGroups_AreRemoved()
        {
            var myGroup = Guid.NewGuid();
            var otherGroup = Guid.NewGuid();

            var node = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Name = "driver",
                PropertyInstance = new List<PropertyInstance>
                {
                    Property(PropertyTemplateType.Password, "secret-password"),
                    Property(PropertyTemplateType.Text, "host.local")
                },
                InverseThis2ParentNodeInstanceNavigation = new List<NodeInstance>
                {
                    new() { ObjId = Guid.NewGuid(), Name = "mine", This2UserGroup = myGroup },
                    new() { ObjId = Guid.NewGuid(), Name = "other", This2UserGroup = otherGroup },
                    new() { ObjId = Guid.NewGuid(), Name = "public" }
                }
            };

            var json = NonAdminResponseFilter.Sanitize(node, Settings, new HashSet<string> { myGroup.ToString() });
            var text = json.ToString(Formatting.None);

            Assert.DoesNotContain("secret-password", text);
            Assert.Contains("host.local", text);

            var children = (JArray)json["inverseThis2ParentNodeInstanceNavigation"];
            Assert.Equal(2, children.Count);
            Assert.DoesNotContain("\"other\"", text);
        }

        [Fact]
        public void NestedLinkToOtherGroup_IsCleared()
        {
            var otherGroup = Guid.NewGuid();
            var visuProperty = Property(PropertyTemplateType.NodeInstance, null);
            visuProperty.ValueNodeInstanceNavigation = new NodeInstance { ObjId = Guid.NewGuid(), Name = "hidden-node", This2UserGroup = otherGroup };

            var json = NonAdminResponseFilter.Sanitize(new List<PropertyInstance> { visuProperty }, Settings, new HashSet<string>());

            Assert.Equal(JTokenType.Null, json[0]["valueNodeInstanceNavigation"].Type);
            Assert.DoesNotContain("hidden-node", json.ToString());
        }
    }
}
