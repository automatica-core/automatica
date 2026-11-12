using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Habits;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Tests.Http;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Push
{
    /// <summary>
    /// A visualization user must not disarm an alarm by writing an input of the security logic: that goes through the
    /// authorised and audited webapi/security-visu. Other logics keep working as before.
    /// </summary>
    public class DataHubSecurityGuardTests
    {
        private readonly Guid _logicId = Guid.NewGuid();
        private readonly Guid _inputId = Guid.NewGuid();
        private readonly Mock<IDispatcher> _dispatcher = new();
        private readonly Mock<IControlCache> _controls = new();
        private readonly Mock<IHabitRecorder> _habits = new();
        private readonly Guid _userId = Guid.NewGuid();

        private DataHub CreateHub(string role)
        {
            var nodes = new Mock<INodeInstanceCache>();
            var logics = new Mock<ILogicInstanceCache>();
            logics.Setup(a => a.Get(_logicId)).Returns(new RuleInstance { ObjId = _logicId });

            var interfaces = new Mock<ILogicInterfaceInstanceCache>();
            interfaces.Setup(a => a.Get(_inputId)).Returns(new RuleInterfaceInstance
            {
                ObjId = _inputId,
                This2RuleInstance = _logicId,
                This2RuleInterfaceTemplateNavigation = new RuleInterfaceTemplate { This2RuleInterfaceDirection = (long)LogicInterfaceDirection.Input }
            });

            var identity = new ClaimsIdentity(new[] { new Claim("name", "tester"), new Claim("role", role), new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) }, "test", "name", "role");
            var context = new Mock<HubCallerContext>();
            context.SetupGet(a => a.User).Returns(new ClaimsPrincipal(identity));

            return new DataHub(_dispatcher.Object, Mock.Of<INotifyDriver>(), nodes.Object, logics.Object, interfaces.Object, _controls.Object,
                _habits.Object, NullLogger<DataHub>.Instance)
            {
                Context = context.Object
            };
        }

        private static JsonElement False => JsonDocument.Parse("false").RootElement;

        private void DispatchCount(Times times) =>
            _dispatcher.Verify(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<DispatchValue>()), times);

        [Theory]
        [InlineData(Role.VisuRole)]
        [InlineData(Role.ViewerRole)]
        public void InputOfSecurityLogic_IsRefusedForNonAdmins(string role)
        {
            _controls.Setup(a => a.Get(_logicId)).Returns(new FakeSecurityControl(_logicId, "alarm"));

            var error = Assert.Throws<HubException>(() => CreateHub(role).SetValue(_inputId, False));

            Assert.Contains("security", error.Message);
            DispatchCount(Times.Never());
        }

        [Fact]
        public void InputOfSecurityLogic_IsWrittenForAdmins()
        {
            _controls.Setup(a => a.Get(_logicId)).Returns(new FakeSecurityControl(_logicId, "alarm"));

            CreateHub(Role.AdminRole).SetValue(_inputId, False);

            DispatchCount(Times.Once());
            // arming an alarm is no habit
            _habits.Verify(a => a.RecordVisu(It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<Guid?>()), Times.Never());
        }

        [Fact]
        public void InputOfOtherLogic_IsStillWrittenByVisu()
        {
            _controls.Setup(a => a.Get(_logicId)).Returns((IControl)null);

            CreateHub(Role.VisuRole).SetValue(_inputId, False);

            DispatchCount(Times.Once());
            _habits.Verify(a => a.RecordVisu(_inputId, false, _userId), Times.Once());
        }
    }
}
