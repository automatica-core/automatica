using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Climate.Common;
using Xunit;

namespace P3.Logic.Operations.Tests.Climate
{
    /// <summary>
    /// Every climate block: unique ids and keys, and every text key (names, descriptions, enum values) translated in en and de
    /// </summary>
    public class ClimateTemplateTests
    {
        private static readonly Type[] Factories = typeof(ClimateLogic).Assembly.GetTypes()
            .Where(t => typeof(LogicFactory).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace!.StartsWith("P3.Logic.Operations.Climate"))
            .OrderBy(t => t.Name)
            .ToArray();

        public static IEnumerable<object[]> FactoryTypes => Factories.Select(t => new object[] { t.Name });

        private static RuleTemplate Template(string factoryName)
        {
            var factory = (LogicFactory)Activator.CreateInstance(Factories.Single(t => t.Name == factoryName))!;
            var mock = new LogicTemplateFactoryMock();
            factory.InitTemplates(mock);
            return mock.GetById(factory.LogicGuid);
        }

        private static JsonElement Locale(string language)
        {
            var assembly = typeof(ClimateLogic).Assembly;
            var name = assembly.GetManifestResourceNames().Single(a => a.EndsWith($"Climate-{language}.json"));
            using var stream = assembly.GetManifestResourceStream(name)!;
            return JsonDocument.Parse(stream).RootElement.Clone();
        }

        private static readonly JsonElement En = Locale("en");
        private static readonly JsonElement De = Locale("de");

        private static bool Translated(JsonElement root, string key)
        {
            var current = root;
            foreach (var part in key.Split('.'))
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                {
                    return false;
                }
            }
            return current.ValueKind == JsonValueKind.String && current.GetString()!.Length > 0;
        }

        private static IEnumerable<string> TextKeys(RuleTemplate template)
        {
            yield return template.Name;
            yield return template.Description;
            yield return template.Group;
            foreach (var i in template.RuleInterfaceTemplate)
            {
                yield return i.Description;
                if (i.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param)
                {
                    yield return i.Name;
                }
                if (i.ParameterDataType == RuleInterfaceParameterDataType.Enum)
                {
                    foreach (var entry in EnumEntries(i.Meta))
                    {
                        yield return entry.Key;
                    }
                }
            }
        }

        private static IEnumerable<(string Value, string Key)> EnumEntries(string meta)
        {
            Assert.StartsWith("ENUM(", meta);
            return meta.Substring(5, meta.Length - 6).Split(';').Select(e => e.Split(',')).Select(e => (e[0], e[1]));
        }

        [Fact]
        public void AllBlocksFound()
        {
            Assert.Equal(14, Factories.Length);
        }

        [Theory]
        [MemberData(nameof(FactoryTypes))]
        public void EveryTextKey_IsTranslated(string factory)
        {
            var template = Template(factory);
            Assert.NotNull(template);
            var missing = TextKeys(template).Distinct()
                .SelectMany(k => new[] { Translated(En, k) ? null : $"en:{k}", Translated(De, k) ? null : $"de:{k}" })
                .Where(a => a != null)
                .ToList();
            Assert.Empty(missing);
        }

        [Theory]
        [MemberData(nameof(FactoryTypes))]
        public void KeysUnique_EnumDefaultsValid(string factory)
        {
            var template = Template(factory);
            var interfaces = template.RuleInterfaceTemplate.ToList();
            Assert.NotEmpty(interfaces);
            Assert.Equal(interfaces.Count, interfaces.Select(a => a.ObjId).Distinct().Count());
            foreach (var group in interfaces.GroupBy(a => a.This2RuleInterfaceDirection))
            {
                Assert.Equal(group.Count(), group.Select(a => a.Key).Distinct().Count());
            }
            foreach (var i in interfaces)
            {
                Assert.Equal(ClimateIds.Of(template.ObjId,
                    (i.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Input ? "in."
                        : i.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Output ? "out." : "param.") + i.Key), i.ObjId);
                if (i.ParameterDataType == RuleInterfaceParameterDataType.Enum)
                {
                    Assert.Contains(i.DefaultValue, EnumEntries(i.Meta).Select(e => e.Value));
                }
            }
        }

        [Fact]
        public void InterfaceIds_UniqueAcrossAllBlocks()
        {
            var ids = Factories.SelectMany(f => Template(f.Name).RuleInterfaceTemplate.Select(i => i.ObjId))
                .Concat(Factories.Select(f => ((LogicFactory)Activator.CreateInstance(f)!).LogicGuid))
                .ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }
}
