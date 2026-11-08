using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Logics;
using Moq;
using Xunit;

namespace P3.Logic.Messenger.Tests
{
    public class MessengerLogicTests
    {
        private const string To = "alice@example.test";

        [Fact]
        public void Trigger_WithRecipient_SendsOneEmail()
        {
            var h = new MessengerTestHarness(To, "Alarm");

            var result = h.Send(true);

            Assert.Empty(result);
            var mail = Assert.Single(h.SentMails);
            Assert.Equal(new[] { To }, mail.to);
            Assert.Equal("Alarm", mail.subject);
        }

        [Fact]
        public void Trigger_ReplacesNameValueAndSourcePlaceholders()
        {
            var h = new MessengerTestHarness(To);
            const string prefix = "Name=Window alarm;Value=42;Source=Kitchen window;Time=";

            h.Send(42, new DispatchableMock("Kitchen window"));

            var body = Assert.Single(h.SentMails).body;
            Assert.StartsWith(prefix, body);
            Assert.DoesNotContain("{{", body);
            // the timestamp format depends on the culture and the current time, only check that it was filled in
            Assert.True(body.Length > prefix.Length);
        }

        [Fact]
        public void Trigger_WithNullSource_ReplacesSourceWithEmpty()
        {
            var h = new MessengerTestHarness(To);

            h.Logic.ValueChanged(h.Trigger, null, "open");

            var body = Assert.Single(h.SentMails).body;
            Assert.Contains("Value=open;Source=;Time=", body);
        }

        [Fact]
        public void Trigger_WithoutRecipients_DoesNotSend()
        {
            var h = new MessengerTestHarness();

            h.Send(true);

            Assert.Empty(h.SentMails);
            h.CloudApi.Verify(a => a.SendEmail(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(";")]
        [InlineData("   ")]
        [InlineData(" ; ;; ")]
        public void Trigger_WithOnlySeparatorsOrBlanks_DoesNotSend(string to)
        {
            var h = new MessengerTestHarness(to);

            h.Send(true);

            Assert.Empty(h.SentMails);
        }

        [Fact]
        public void Recipients_AreSplitBySemicolonTrimmedAndEmptiesRemoved()
        {
            var h = new MessengerTestHarness(" a@example.test ;b@example.test;; ;c@example.test ");

            h.Send(1);

            Assert.Equal(new[] { "a@example.test", "b@example.test", "c@example.test" }, Assert.Single(h.SentMails).to);
        }

        [Fact]
        public void Subject_Empty_UsesTheDefaultSubject()
        {
            // the template default is "", the parameter description promises "Automatica.Core Message" then
            var h = new MessengerTestHarness(To);

            h.Send(true);

            Assert.Equal("Automatica.Core Message", Assert.Single(h.SentMails).subject);
        }

        [Fact]
        public void BodyTemplate_ReplacesTheDefaultText_AndPlaceholdersWorkInTheSubject()
        {
            var h = new MessengerTestHarness(To, "{{NAME}}: {{VALUE}}");
            h.Logic.ValueChanged(h.Param(MessengerFactory.BodyProperty), new DispatchableMock(), "Temperature {{VALUE}} °C from {{SOURCE}} on {{DATE}}");

            h.Send(23.5, new DispatchableMock("Living room"));

            var mail = Assert.Single(h.SentMails);
            Assert.Equal("Window alarm: " + 23.5.ToString(), mail.subject);
            Assert.StartsWith("Temperature " + 23.5.ToString() + " °C from Living room on ", mail.body);
            Assert.DoesNotContain("{{", mail.body);
        }

        [Fact]
        public void BodyTemplate_Empty_FallsBackToTheTranslation()
        {
            var h = new MessengerTestHarness(To);
            h.Logic.ValueChanged(h.Param(MessengerFactory.BodyProperty), new DispatchableMock(), "  ");

            h.Send(1, new DispatchableMock("Kitchen window"));

            Assert.StartsWith("Name=Window alarm;Value=1;Source=Kitchen window;Time=", Assert.Single(h.SentMails).body);
        }

        [Fact]
        public void Trigger_NullValue_DoesNotSend()
        {
            var h = new MessengerTestHarness(To);

            h.Send(null);

            Assert.Empty(h.SentMails);
        }

        [Fact]
        public void Trigger_SameBoxedBoolTwice_SendsOnlyOnce()
        {
            var h = new MessengerTestHarness(To);

            // two different boxes with the same value - must be compared by value
            h.Send((object)true);
            h.Send((object)true);

            Assert.Single(h.SentMails);
        }

        [Fact]
        public void Trigger_SameBoxedNumberTwice_SendsOnlyOnce()
        {
            var h = new MessengerTestHarness(To);

            h.Send((object)5.5d);
            h.Send((object)5.5d);

            Assert.Single(h.SentMails);
        }

        [Fact]
        public void Trigger_ChangedValue_SendsAgain()
        {
            var h = new MessengerTestHarness(To);

            h.Send(true);
            h.Send(false);
            h.Send(true);

            Assert.Equal(3, h.SentMails.Count);
            Assert.Equal(new[] { "Value=True", "Value=False", "Value=True" },
                h.SentMails.Select(m => m.body.Split(';')[1]));
        }

        [Fact]
        public void Trigger_NullBetweenSameValues_SendsTheValueAgain()
        {
            var h = new MessengerTestHarness(To);

            h.Send("x");
            h.Send(null);
            h.Send("x");

            Assert.Equal(2, h.SentMails.Count);
        }

        [Fact]
        public void Trigger_DifferentTypesSameText_AreDifferentValues()
        {
            // 1 (int) and "1" (string) are not Equal - both send
            var h = new MessengerTestHarness(To);

            h.Send(1);
            h.Send("1");

            Assert.Equal(2, h.SentMails.Count);
        }

        [Fact]
        public void Trigger_WhenCloudApiThrows_DoesNotThrowAndRemembersValue()
        {
            var h = new MessengerTestHarness(To);
            h.CloudApi.Setup(a => a.SendEmail(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("cloud down"));

            var result = h.Send(true);
            Assert.Empty(result);

            // the value is remembered although sending failed, the same value is not retried
            h.Send(true);
            h.CloudApi.Verify(a => a.SendEmail(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Trigger_WhenCloudApiReturnsFaultedTask_DoesNotThrow()
        {
            var h = new MessengerTestHarness(To);
            h.CloudApi.Setup(a => a.SendEmail(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromException<bool>(new InvalidOperationException("cloud down")));

            var ex = Record.Exception(() => h.Send(true));

            Assert.Null(ex);
        }

        [Fact]
        public void Trigger_WhenTranslationMissing_DoesNotSendAndDoesNotThrow()
        {
            var h = new MessengerTestHarness(To, body: null);

            var ex = Record.Exception(() => h.Send(true));

            Assert.Null(ex);
            Assert.Empty(h.SentMails);
        }

        [Fact]
        public void ToParameterChange_UpdatesRecipients()
        {
            var h = new MessengerTestHarness(To);

            h.Logic.ValueChanged(h.Param(MessengerFactory.ToProperty), new DispatchableMock(), "bob@example.test;carol@example.test");
            h.Send(true);

            Assert.Equal(new[] { "bob@example.test", "carol@example.test" }, Assert.Single(h.SentMails).to);
        }

        [Fact]
        public void ToParameterChange_ToNull_DisablesSending()
        {
            var h = new MessengerTestHarness(To);

            h.Logic.ValueChanged(h.Param(MessengerFactory.ToProperty), new DispatchableMock(), (object)null);
            h.Send(true);

            Assert.Empty(h.SentMails);
        }

        [Fact]
        public void SubjectParameterChange_UpdatesSubject()
        {
            var h = new MessengerTestHarness(To, "Old");

            h.Logic.ValueChanged(h.Param(MessengerFactory.SubjectProperty), new DispatchableMock(), "New");
            h.Send(true);

            Assert.Equal("New", Assert.Single(h.SentMails).subject);
        }

        [Fact]
        public void ParameterChange_ReturnsNoOutputsAndSendsNothing()
        {
            var h = new MessengerTestHarness(To);

            var result = h.Logic.ValueChanged(h.Param(MessengerFactory.SubjectProperty), new DispatchableMock(), "S");

            Assert.Empty(result);
            Assert.Empty(h.SentMails);
        }

        [Fact]
        public async Task Start_RepushesParameters_SoChangedInstanceValuesWin()
        {
            var h = new MessengerTestHarness(To, "Before");

            // the rule instance is edited (reload) - start pushes the parameter values again
            h.Param(MessengerFactory.ToProperty).Value = "new@example.test";
            h.Param(MessengerFactory.SubjectProperty).Value = "After";
            await h.Logic.Start();

            h.Send(true);

            var mail = Assert.Single(h.SentMails);
            Assert.Equal(new[] { "new@example.test" }, mail.to);
            Assert.Equal("After", mail.subject);
        }
    }

    public class MessengerFactoryTests
    {
        private static (MessengerFactory factory, LogicTemplateFactoryMock templates) Create()
        {
            var factory = new MessengerFactory();
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            return (factory, templates);
        }

        [Fact]
        public void Metadata_IsStable()
        {
            var factory = new MessengerFactory();

            Assert.Equal("Messenger", factory.LogicName);
            Assert.Equal(new Guid("215efbd7-95e6-4451-9837-612010f493a6"), factory.LogicGuid);
            Assert.Equal(new Version(0, 1, 0, 1), factory.LogicVersion);
        }

        [Fact]
        public void InitTemplates_CreatesTriggerInputAndThreeTextParameters()
        {
            var (factory, templates) = Create();

            var template = templates.GetById(factory.LogicGuid);
            Assert.Equal("messenger-cloud-email", template.Key);
            Assert.Equal(4, template.RuleInterfaceTemplate.Count);

            var trigger = template.RuleInterfaceTemplate.Single(a => a.ObjId == MessengerTestHarness.TriggerInput);
            Assert.Equal((long)LogicInterfaceDirection.Input, trigger.This2RuleInterfaceDirection);

            foreach (var id in new[] { MessengerFactory.ToProperty, MessengerFactory.SubjectProperty, MessengerFactory.BodyProperty })
            {
                var p = template.RuleInterfaceTemplate.Single(a => a.ObjId == id);
                Assert.Equal((long)LogicInterfaceDirection.Param, p.This2RuleInterfaceDirection);
                Assert.Equal(RuleInterfaceParameterDataType.Text, p.ParameterDataType);
                Assert.Equal(string.Empty, p.DefaultValue);
            }
        }

        [Fact]
        public void InitTemplates_HasNoOutputs()
        {
            var (factory, templates) = Create();

            Assert.DoesNotContain(templates.GetById(factory.LogicGuid).RuleInterfaceTemplate,
                a => a.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Output);
        }

        [Fact]
        public void CreateLogicInstance_ReturnsMessengerLogic()
        {
            var h = new MessengerTestHarness();

            Assert.IsType<MessengerLogic>(h.Logic);
        }
    }
}
