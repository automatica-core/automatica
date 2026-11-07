using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Localization;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using Moq;

namespace P3.Logic.Messenger.Tests
{
    /// <summary>
    /// Builds a <see cref="MessengerLogic"/> on a mocked <see cref="ILogicContext"/> so the cloud api calls can be observed.
    /// Nothing is ever sent: <see cref="IServerCloudApi"/> is a strict-enough Moq mock.
    /// </summary>
    internal sealed class MessengerTestHarness
    {
        /// <summary>the trigger input template id (not exposed by the factory)</summary>
        public static readonly Guid TriggerInput = new("7ac55e7b-3837-4efb-8995-70ef4c4dfef2");

        public const string BodyTemplate = "Name={{NAME}};Value={{VALUE}};Source={{SOURCE}};Time={{TIMESTAMP}}";

        public Mock<IServerCloudApi> CloudApi { get; } = new();
        public Mock<ILocalizationProvider> Localization { get; } = new();
        public ILogicContext Context { get; }
        public RuleInstance RuleInstance { get; }
        public MessengerLogic Logic { get; private set; }

        public List<(IList<string> to, string subject, string body)> SentMails { get; } = new();

        public MessengerTestHarness(string to = null, string subject = null, string body = BodyTemplate)
        {
            var factory = new MessengerFactory();
            var templateFactory = new LogicTemplateFactoryMock();
            factory.InitTemplates(templateFactory);

            RuleInstance = templateFactory.CreateRuleInstanceFromTemplate(factory.LogicGuid);
            RuleInstance.Name = "Window alarm";

            if (to != null)
            {
                Param(MessengerFactory.ToProperty).Value = to;
            }
            if (subject != null)
            {
                Param(MessengerFactory.SubjectProperty).Value = subject;
            }

            Localization.Setup(a => a.GetTranslation("MESSENGER.CLOUD_EMAIL.BODY")).Returns(body);
            CloudApi.Setup(a => a.SendEmail(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<IList<string>, string, string>((t, s, b) => SentMails.Add((t.ToList(), s, b)))
                .ReturnsAsync(true);

            Context = new TestLogicContext(RuleInstance, templateFactory, CloudApi.Object, Localization.Object);

            Logic = (MessengerLogic)factory.CreateLogicInstance(Context);
        }

        public RuleInterfaceInstance Param(Guid template) =>
            RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == template);

        public RuleInterfaceInstance Trigger =>
            RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == TriggerInput);

        public IList<ILogicOutputChanged> Send(object value, IDispatchable source = null) =>
            Logic.ValueChanged(Trigger, source ?? new DispatchableMock("Window sensor"), value);
    }
}
