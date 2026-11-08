using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using Newtonsoft.Json.Linq;
using P3.Logic.Surveillance.AccessController;
using P3.Logic.Surveillance.AlarmChain;
using P3.Logic.Surveillance.BurglarAlarm;
using P3.Logic.Surveillance.Caller;
using P3.Logic.Surveillance.EmergencyAlarm;
using P3.Logic.Surveillance.FireWaterAlarm;
using P3.Logic.Surveillance.Sia;
using Xunit;

namespace P3.Logic.Surveillance.Tests
{
    /// <summary>
    /// Every key of the security blocks (templates, visualization texts, log events) exists in English and German
    /// </summary>
    public class TranslationTests
    {
        private static readonly LogicFactory[] Factories =
        {
            new BurglarAlarmLogicFactory(), new BurglarAlarmCentralLogicFactory(), new FireWaterAlarmLogicFactory(),
            new EmergencyAlarmLogicFactory(), new AlarmChainLogicFactory(), new CallerLogicFactory(), new SiaLogicFactory(),
            new AccessControllerLogicFactory()
        };

        private static IEnumerable<string> TemplateKeys()
        {
            foreach (var factory in Factories)
            {
                var templates = new LogicTemplateFactoryMock();
                factory.InitTemplates(templates);
                var instance = templates.CreateRuleInstanceFromTemplate(factory.LogicGuid);
                yield return instance.This2RuleTemplateNavigation.Name;
                yield return instance.This2RuleTemplateNavigation.Description;
                foreach (var i in instance.RuleInterfaceInstance)
                {
                    var t = i.This2RuleInterfaceTemplateNavigation;
                    if (t.Name.StartsWith("SURVEILLANCE.", StringComparison.Ordinal))
                    {
                        yield return t.Name;
                    }
                    yield return t.Description;
                }
            }
        }

        private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
            Path.Combine(Path.GetDirectoryName(path)!, "..", "P3.Logic.Surveillance");

        /// <summary>
        /// Literal keys in the code (visualization texts, causes, notifications) and the log events
        /// </summary>
        private static IEnumerable<string> CodeKeys()
        {
            var files = Directory.GetFiles(SourceDirectory(), "*.cs", SearchOption.AllDirectories)
                .Where(a => !a.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !a.Contains("WindowMonitor"));
            foreach (var file in files)
            {
                var code = File.ReadAllText(file);
                foreach (Match m in Regex.Matches(code, "\"(SURVEILLANCE\\.(VISU|NOTIFY|ACCESS|BURGLAR_ALARM\\.TEXT)\\.[A-Z0-9_.]+[A-Z0-9_])\""))
                {
                    yield return m.Groups[1].Value;
                }
                foreach (Match m in Regex.Matches(code, "AddLog\\(\"([a-z0-9]+(?:-[a-z0-9]+)*)\""))
                {
                    yield return EventKey(m.Groups[1].Value);
                }
                foreach (Match m in Regex.Matches(code, "AddLog\\(\"central-\" \\+ command"))
                {
                    foreach (var c in new[] { "arm", "arm-np", "arm-delayed", "arm-delayed-np", "disarm", "acknowledge" })
                    {
                        yield return EventKey("central-" + c);
                    }
                }
                foreach (Match m in Regex.Matches(code, "AddLog\\(\"denied-\" \\+ reason"))
                {
                    foreach (var r in new[] { "lockout", "locked", "unknown-code", "unknown-tag", "factors-mismatch", "outside-time", "tag-not-allowed", "code-not-allowed" })
                    {
                        yield return EventKey("denied-" + r);
                        yield return "SURVEILLANCE.ACCESS.CAUSE." + r.ToUpperInvariant().Replace('-', '_');
                    }
                }
            }

            // events written through variables
            foreach (var e in new[] { "fire", "water", "granted-code", "granted-tag", "armed", "armed-np", "locked", "unlocked", "user-created", "user-changed", "sia-sent", "sia-sent-backup" })
            {
                yield return EventKey(e);
            }
        }

        internal static string EventKey(string e) => "SURVEILLANCE.EVENT." + e.ToUpperInvariant().Replace('-', '_');

        private static JObject Load(string language)
        {
            using var stream = typeof(BurglarAlarmLogicFactory).Assembly.GetManifestResourceStream($"P3.Logic.Surveillance.Resources.Surveillance-{language}.json")!;
            using var reader = new StreamReader(stream);
            return JObject.Parse(reader.ReadToEnd());
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void AllKeysAreTranslated(string language)
        {
            var json = Load(language);
            var missing = TemplateKeys().Concat(CodeKeys())
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .Where(key => json.SelectToken(key) is not JValue { Type: JTokenType.String })
                .OrderBy(a => a)
                .ToList();

            Assert.True(missing.Count == 0, $"Missing in {language}:\n" + string.Join("\n", missing));
        }
    }
}
