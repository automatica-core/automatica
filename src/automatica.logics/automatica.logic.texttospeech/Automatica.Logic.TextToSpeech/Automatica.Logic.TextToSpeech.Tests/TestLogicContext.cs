using Automatica.Core.Base.IO;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;

namespace Automatica.Logic.TextToSpeech.Tests
{
    /// <summary>
    /// <see cref="ILogicContext"/> has an internal setter (RuleInstance), so neither Moq nor a plain class outside of
    /// Automatica.Core.UnitTests.Base can implement it. This class re-implements the interface on top of
    /// <see cref="LogicContextMock"/> (which implements the internal setter) and replaces the cloud api with a test double.
    /// </summary>
    public class TestLogicContext : LogicContextMock, ILogicContext
    {
        public TestLogicContext(RuleInstance instance, ILogicTemplateFactory factory, IDispatcher dispatcher, IServerCloudApi cloudApi)
            : base(instance, factory, dispatcher)
        {
            CloudApi = cloudApi;
        }

        public new IServerCloudApi CloudApi { get; }
    }
}
