using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Tests.Logic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// A security logic as the api sees it: "arm" works, "busy" is not possible, "manage" is for administrators only
    /// </summary>
    internal sealed class FakeSecurityControl : ISecurityControl
    {
        public FakeSecurityControl(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string SecurityKind => "burglar";
        public IReadOnlyCollection<string> SecurityCommands { get; } = new[] { "arm", "busy", "manage" };

        public string Status { get; private set; } = "disarmed";
        public SecurityCommandContext LastContext { get; private set; }
        public IReadOnlyDictionary<string, string> LastArguments { get; private set; }

        public object GetSecurityState(bool includeAdminData)
        {
            return includeAdminData
                ? new { status = Status, users = new[] { new { name = "Anna", hasCode = true } } }
                : new { status = Status };
        }

        public Task<SecurityCommandResult> ExecuteSecurityCommandAsync(string command, IReadOnlyDictionary<string, string> arguments,
            SecurityCommandContext context, CancellationToken token = default)
        {
            LastContext = context;
            LastArguments = arguments;
            var result = command switch
            {
                "arm" => SecurityCommandResult.Ok,
                "busy" => SecurityCommandResult.NotPossible,
                "manage" => context.IsAdmin ? SecurityCommandResult.Ok : SecurityCommandResult.Denied,
                _ => SecurityCommandResult.UnknownCommand
            };
            if (result == SecurityCommandResult.Ok && command == "arm")
            {
                Status = "armed";
            }
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Keeps every formatted log line of the host, to check that no code ends up in a log
    /// </summary>
    internal sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Lines { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class Logger(CapturingLoggerProvider provider, string category) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                provider.Lines.Enqueue($"{category}: {formatter(state, exception)} {exception}");
            }
        }
    }

    public class SecurityVisuHttpTests : IDisposable
    {
        private const string Code = "918273";

        private readonly WebApiHost _host;
        private readonly CapturingLoggerProvider _logs = new();
        private readonly Guid _ownGroup;
        private readonly Guid _otherGroup;

        public SecurityVisuHttpTests()
        {
            // the host logs warnings only: everything of the security api goes to the capture, down to debug
            _host = new WebApiHost(configureServices: services => services.AddLogging(a => a
                .AddProvider(_logs)
                .AddFilter<CapturingLoggerProvider>(typeof(Controllers.SecurityVisuController).FullName, LogLevel.Trace)));

            _ownGroup = _host.CreateUserGroup("sec-own");
            _otherGroup = _host.CreateUserGroup("sec-other");

            using var db = _host.CreateDbContext();
            var factory = new LogicTemplateFactory(NullLogger<LogicTemplateFactory>.Instance, db, _host.Configuration) { Owner = Guid.NewGuid() };
            new TestLogicFactory().InitTemplates(factory);
        }

        public void Dispose() => _host.Dispose();

        /// <summary>
        /// A logic of the user group (none: visible to all) that is a security control
        /// </summary>
        private FakeSecurityControl AddControl(Guid? userGroup)
        {
            using (var db = _host.CreateDbContext())
            {
                var logic = new RuleInstance
                {
                    ObjId = Guid.NewGuid(),
                    Name = "alarm",
                    Description = "",
                    VisuName = "",
                    This2RuleTemplate = new TestLogicFactory().LogicGuid,
                    This2RulePage = db.RulePages.First().ObjId,
                    This2UserGroup = userGroup,
                    CreatedAt = DateTimeOffset.Now,
                    ModifiedAt = DateTimeOffset.Now
                };
                db.RuleInstances.Add(logic);
                db.SaveChanges();

                var control = new FakeSecurityControl(logic.ObjId, "Alarm " + logic.ObjId.ToString("N")[..6]);
                _host.GetService<ILogicCacheFacade>().ClearInstances();
                _host.GetService<ILogicInstanceCache>().Clear();
                _host.GetService<IControlCache>().Add(control.Id, control);
                return control;
            }
        }

        private HttpClient Viewer()
        {
            var userId = _host.CreateUser(_host.UniqueName("viewer"), WebApiHost.StrongPassword, new[] { Role.ViewerRole }, new[] { _ownGroup });
            return _host.CreateClient(_host.TokenFor(userId));
        }

        private HttpClient Visu() => _host.CreateClient(_host.CreateVisuToken(_ownGroup));

        private HttpClient Admin() => _host.CreateClient(_host.CreateAdminToken());

        private static Task<HttpResponseMessage> Command(HttpClient client, Guid id, string command, object arguments = null) =>
            client.PostJson($"webapi/security-visu/{id}/command", new { command, arguments });

        [Fact]
        public async Task Viewer_ReadsTheState_ButMayNotCommand()
        {
            var control = AddControl(_ownGroup);
            var viewer = Viewer();

            var response = await viewer.GetAsync($"webapi/security-visu/{control.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.ReadJson();
            Assert.Equal("burglar", (string)body["kind"]);
            Assert.False((bool)body["canCommand"]);
            Assert.False((bool)body["isAdmin"]);
            Assert.Equal("disarmed", (string)body["state"]["status"]);
            Assert.Null(body["state"]["users"]);

            Assert.Equal(HttpStatusCode.Forbidden, (await Command(viewer, control.Id, "arm")).StatusCode);
            Assert.Equal("disarmed", control.Status);
            Assert.Null(control.LastContext);
        }

        [Fact]
        public async Task Visu_Arms_AndTheLogicSeesWhoDidIt()
        {
            var control = AddControl(_ownGroup);
            var visu = Visu();

            var state = await (await visu.GetAsync($"webapi/security-visu/{control.Id}")).ReadJson();
            Assert.True((bool)state["canCommand"]);
            Assert.False((bool)state["isAdmin"]);

            var response = await Command(visu, control.Id, "arm");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("armed", (string)(await response.ReadJson())["state"]["status"]);
            Assert.False(control.LastContext.IsAdmin);
            Assert.StartsWith("visu", control.LastContext.UserName);
        }

        [Fact]
        public async Task ManagementCommand_IsForbiddenForVisu_AndAllowedForAdmins()
        {
            var control = AddControl(_ownGroup);

            Assert.Equal(HttpStatusCode.Forbidden, (await Command(Visu(), control.Id, "manage")).StatusCode);

            var admin = Admin();
            var response = await Command(admin, control.Id, "manage");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(control.LastContext.IsAdmin);
            Assert.NotNull((await response.ReadJson())["state"]["users"]);

            var state = await (await admin.GetAsync($"webapi/security-visu/{control.Id}")).ReadJson();
            Assert.True((bool)state["isAdmin"]);
            Assert.NotNull(state["state"]["users"]);
        }

        [Fact]
        public async Task NotPossible_Returns409WithTheState_UnknownOrEmptyCommand_Returns400()
        {
            var control = AddControl(_ownGroup);
            var visu = Visu();

            var busy = await Command(visu, control.Id, "busy");
            Assert.Equal(HttpStatusCode.Conflict, busy.StatusCode);
            Assert.Equal("disarmed", (string)(await busy.ReadJson())["state"]["status"]);

            Assert.Equal(HttpStatusCode.BadRequest, (await Command(visu, control.Id, "self-destruct")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Command(visu, control.Id, " ")).StatusCode);
        }

        [Fact]
        public async Task LogicOfOtherGroup_LooksUnknown()
        {
            var other = AddControl(_otherGroup);
            var open = AddControl(null);
            var visu = Visu();

            Assert.Equal(HttpStatusCode.NotFound, (await visu.GetAsync($"webapi/security-visu/{other.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Command(visu, other.Id, "arm")).StatusCode);
            Assert.Null(other.LastContext);

            Assert.Equal(HttpStatusCode.OK, (await visu.GetAsync($"webapi/security-visu/{open.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await visu.GetAsync($"webapi/security-visu/{Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task Anonymous_IsRejected()
        {
            var control = AddControl(null);
            var anonymous = _host.CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"webapi/security-visu/{control.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Command(anonymous, control.Id, "arm")).StatusCode);
            Assert.Null(control.LastContext);
        }

        [Fact]
        public async Task CommandArguments_ReachTheLogic_ButNeverTheLog()
        {
            var control = AddControl(_ownGroup);

            var response = await Command(Admin(), control.Id, "manage", new { name = "Anna", code = Code, tag = "04A1B2C3" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(Code, control.LastArguments["code"]);
            Assert.Contains(_logs.Lines, a => a.Contains("Security command manage") && a.Contains(control.Name));
            Assert.DoesNotContain(_logs.Lines, a => a.Contains(Code) || a.Contains("04A1B2C3"));
        }
    }
}
