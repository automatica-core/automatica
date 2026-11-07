using Automatica.Core.Base.IO;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;

namespace P3.Logic.Messenger.Tests
{
    /// <summary>
    /// <see cref="ILogicContext"/> has an internal setter (RuleInstance), so neither Moq nor a plain class outside of
    /// Automatica.Core.UnitTests.Base can implement it. This class re-implements the interface on top of
    /// <see cref="LogicContextMock"/> (which implements the internal setter) and replaces the cloud api and the
    /// localization provider with test doubles.
    /// </summary>
    public class TestLogicContext : LogicContextMock, ILogicContext
    {
        public TestLogicContext(RuleInstance instance, ILogicTemplateFactory factory, IServerCloudApi cloudApi, ILocalizationProvider localization)
            : base(instance, factory, new Moq.Mock<IDispatcher>().Object)
        {
            CloudApi = cloudApi;
            LocalizationProvider = localization;
        }

        public new IServerCloudApi CloudApi { get; }

        public new ILocalizationProvider LocalizationProvider { get; }
    }
}
