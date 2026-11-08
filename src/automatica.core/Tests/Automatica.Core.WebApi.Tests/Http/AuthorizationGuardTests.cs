using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// Guard tests over all web api controllers: only the intended actions are anonymous, every other action needs a
    /// token, a role policy and a full (not "must change password") token.
    /// A new controller or action must be added consciously to the anonymous list.
    /// </summary>
    public class AuthorizationGuardTests : IClassFixture<WebApiHostFixture>
    {
        /// <summary>
        /// The only actions callable without a token: login/logout, the translations, the language and the version.
        /// </summary>
        private static readonly HashSet<string> IntendedAnonymousActions = new()
        {
            $"{nameof(AuthenticationController)}.{nameof(AuthenticationController.Login)}",
            $"{nameof(AuthenticationController)}.{nameof(AuthenticationController.LogOut)}",
            // both overloads (all translations, one language)
            $"{nameof(LocalizationController)}.Get",
            $"{nameof(SettingsController)}.{nameof(SettingsController.GetLanguageSetting)}",
            $"{nameof(VersionController)}.Get",
        };

        /// <summary>
        /// Non-admin actions (viewer policy or the own password): everything else requires the administrator role.
        /// </summary>
        private static readonly HashSet<string> IntendedNonAdminActions = new()
        {
            $"{nameof(AccountController)}.{nameof(AccountController.ChangePassword)}",
            $"{nameof(AreaController)}.{nameof(AreaController.GetTemplates)}",
            $"{nameof(AreaController)}.{nameof(AreaController.GetInstances)}",
            $"{nameof(CameraController)}.{nameof(CameraController.Info)}",
            $"{nameof(CameraController)}.{nameof(CameraController.Snapshot)}",
            $"{nameof(CameraController)}.{nameof(CameraController.Live)}",
            $"{nameof(CategoryController)}.{nameof(CategoryController.GetTemplates)}",
            $"{nameof(CategoryController)}.{nameof(CategoryController.GetInstances)}",
            $"{nameof(DataController)}.{nameof(DataController.GetCurrentNodeValues)}",
            $"{nameof(DataController)}.{nameof(DataController.GetNodeValue)}",
            $"{nameof(DataController)}.{nameof(DataController.GetCurrentValues)}",
            $"{nameof(DataController)}.{nameof(DataController.GetValue)}",
            $"{nameof(DataController)}.{nameof(DataController.GetTrendingValues)}",
            $"{nameof(HyperSeriesController)}.{nameof(HyperSeriesController.GetHourlyValues)}",
            $"{nameof(LogicsController)}.{nameof(LogicsController.GetInstanceData)}",
            $"{nameof(NodeInstanceController)}.{nameof(NodeInstanceController.GetSingle)}",
            $"{nameof(PingController)}.{nameof(PingController.Ping)}",
            $"{nameof(ServerController)}.{nameof(ServerController.GetServerStatus)}",
            $"{nameof(SettingsController)}.{nameof(SettingsController.GetSetting)}",
            $"{nameof(VisualizationController)}.{nameof(VisualizationController.GetTemplates)}",
            $"{nameof(VisualizationController)}.{nameof(VisualizationController.GetPages)}",
            $"{nameof(VisualizationController)}.{nameof(VisualizationController.LoadDefaultPage)}",
            $"{nameof(VisualizationController)}.{nameof(VisualizationController.GetAllByCategory)}",
            $"{nameof(VisualizationController)}.{nameof(VisualizationController.GetAllByArea)}",
            $"{nameof(VisualizationController)}.{nameof(VisualizationController.GetPage)}",
            $"{nameof(ShadingVisuController)}.{nameof(ShadingVisuController.GetBlinds)}",
            // automations an admin released for the visualization: every signed-in user sees them
            $"{nameof(AutomationsVisuController)}.{nameof(AutomationsVisuController.GetAll)}",
        };

        /// <summary>
        /// Actions for the visualization and the administrators (not for viewers): the auto switch of the shading on the
        /// blind tiles, and changing the released values of automations (Loxone "Automatic Rule": times, days,
        /// thresholds and values the admin marked, on/off; never the structure). They need the roles visu or
        /// administrator on top of the viewer policy of their controller.
        /// </summary>
        private static readonly HashSet<string> IntendedVisuActions = new()
        {
            $"{nameof(ShadingVisuController)}.{nameof(ShadingVisuController.SetAutomatic)}",
            $"{nameof(ShadingVisuController)}.{nameof(ShadingVisuController.Resume)}",
            $"{nameof(AutomationsVisuController)}.{nameof(AutomationsVisuController.Update)}",
            $"{nameof(AutomationsVisuController)}.{nameof(AutomationsVisuController.SetEnabled)}",
        };

        private readonly WebApiHost _host;

        public AuthorizationGuardTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        private static IEnumerable<(Type controller, MethodInfo action)> AllActions()
        {
            var controllers = typeof(BaseController).Assembly.GetTypes()
                .Where(a => typeof(ControllerBase).IsAssignableFrom(a) && !a.IsAbstract && a.IsPublic);

            foreach (var controller in controllers)
            {
                foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                             .Where(a => !a.IsSpecialName && a.GetCustomAttribute<NonActionAttribute>() == null))
                {
                    yield return (controller, action);
                }
            }
        }

        private static string Name((Type controller, MethodInfo action) a) => $"{a.controller.Name}.{a.action.Name}";

        private static bool IsAnonymous((Type controller, MethodInfo action) a) =>
            a.action.GetCustomAttribute<AllowAnonymousAttribute>() != null || a.controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;

        private static List<AuthorizeAttribute> Authorize((Type controller, MethodInfo action) a) =>
            a.action.GetCustomAttributes<AuthorizeAttribute>().Concat(a.controller.GetCustomAttributes<AuthorizeAttribute>()).ToList();

        private static bool RequiresAdmin((Type controller, MethodInfo action) a) =>
            Authorize(a).Any(x => x.Policy == Role.AdminRole || (x.Roles ?? "").Split(',').Select(r => r.Trim()).Contains(Role.AdminRole));

        private static IEnumerable<string> Roles(AuthorizeAttribute a) =>
            (a.Roles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        [Fact]
        public void Controllers_AreFound()
        {
            // sanity check of the reflection below
            Assert.True(AllActions().Count() > 80);
        }

        [Fact]
        public void AnonymousActions_AreExactlyTheIntendedOnes()
        {
            var anonymous = AllActions().Where(IsAnonymous).Select(Name).Distinct().OrderBy(a => a).ToList();
            Assert.Equal(IntendedAnonymousActions.OrderBy(a => a).ToList(), anonymous);
        }

        [Fact]
        public void EveryOtherAction_HasAnExplicitPolicyOrRole()
        {
            // the global AuthorizeFilter only requires a token, the roles must be explicit
            var missing = AllActions().Where(a => !IsAnonymous(a))
                .Where(a => !Authorize(a).Any(x => !string.IsNullOrEmpty(x.Policy) || !string.IsNullOrEmpty(x.Roles)))
                .Select(Name)
                .ToList();
            Assert.Empty(missing);
        }

        [Fact]
        public void EveryAction_IsAdminOnly_UnlessIntendedForViewers()
        {
            var nonAdmin = AllActions().Where(a => !IsAnonymous(a) && !RequiresAdmin(a)).Select(Name).Distinct().OrderBy(a => a).ToList();
            Assert.Equal(IntendedNonAdminActions.OrderBy(a => a).ToList(), nonAdmin);
        }

        [Fact]
        public void VisuActions_AreExactlyTheIntendedOnes()
        {
            // an action that lets the visu role in next to the administrators
            var visu = AllActions().Where(a => Authorize(a).Any(x => Roles(x).Contains(Role.VisuRole))).Select(Name).Distinct().OrderBy(a => a).ToList();
            Assert.Equal(IntendedVisuActions.OrderBy(a => a).ToList(), visu);

            foreach (var action in AllActions().Where(a => IntendedVisuActions.Contains(Name(a))))
            {
                var authorize = Authorize(action);
                Assert.Contains(authorize, a => a.Policy == Role.ViewerRole);
                Assert.Contains(authorize, a => Roles(a).OrderBy(r => r).SequenceEqual(new[] { Role.AdminRole, Role.VisuRole }.OrderBy(r => r)));
            }
        }

        [Fact]
        public void NonAdminActions_UseTheViewerPolicy_ExceptThePasswordChange()
        {
            foreach (var action in AllActions().Where(a => IntendedNonAdminActions.Contains(Name(a))))
            {
                var policies = Authorize(action).Select(a => a.Policy).Where(a => a != null).ToList();
                var expected = Name(action) == $"{nameof(AccountController)}.{nameof(AccountController.ChangePassword)}"
                    ? AccountController.PasswordChangePolicy
                    : Role.ViewerRole;
                Assert.True(policies.Contains(expected), $"{Name(action)} should use the {expected} policy");
            }
        }

        [Fact]
        public void LoginAndPasswordChange_AreRateLimited()
        {
            var login = typeof(AuthenticationController).GetMethod(nameof(AuthenticationController.Login));
            var change = typeof(AccountController).GetMethod(nameof(AccountController.ChangePassword));
            Assert.Equal(AuthenticationController.LoginRateLimitPolicy,
                login!.GetCustomAttribute<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()?.PolicyName);
            Assert.Equal(AuthenticationController.LoginRateLimitPolicy,
                change!.GetCustomAttribute<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()?.PolicyName);
        }

        private List<(string method, string url, ControllerActionDescriptor action, IReadOnlyList<IAuthorizeData> authorize, bool anonymous)> Endpoints(bool includeBaseControllerMethods = false)
        {
            var sources = _host.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();
            var ret = new List<(string, string, ControllerActionDescriptor, IReadOnlyList<IAuthorizeData>, bool)>();
            foreach (var endpoint in sources.SelectMany(a => a.Endpoints).OfType<RouteEndpoint>())
            {
                var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                // the public helpers of the base controller are routed as well (see BaseControllerHelpers_AreNotRoutedAsActions)
                if (action == null || (!includeBaseControllerMethods && action.MethodInfo.DeclaringType == typeof(BaseController)))
                {
                    continue;
                }

                var url = Regex.Replace(endpoint.RoutePattern.RawText ?? "", "\\{[^}]+\\}", Guid.NewGuid().ToString());
                var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? new[] { "GET" };
                foreach (var method in methods)
                {
                    ret.Add((method, url, action, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                        endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null));
                }
            }

            return ret;
        }

        private static HttpRequestMessage Request(string method, string url) =>
            new(new HttpMethod(method), url) { Content = HttpTestExtensions.RawJson("{}") };

        [Fact]
        public void BaseControllerHelpers_AreNotRoutedAsActions()
        {
            var routed = Endpoints(includeBaseControllerMethods: true)
                .Where(a => a.action.MethodInfo.DeclaringType == typeof(BaseController))
                .Select(a => $"{a.method} {a.url} -> {a.action.MethodInfo.Name}")
                .ToList();
            Assert.Empty(routed);
        }

        [Fact]
        public void Endpoints_CoverAllActions()
        {
            var routed = Endpoints().Select(a => $"{a.action.ControllerTypeInfo.Name}.{a.action.MethodInfo.Name}").ToHashSet();
            var missing = AllActions().Select(Name).Where(a => !routed.Contains(a)).ToList();
            Assert.Empty(missing);
        }

        [Fact]
        public async Task EveryProtectedEndpoint_WithoutToken_Returns401()
        {
            var client = _host.CreateClient();
            var failures = new List<string>();

            foreach (var (method, url, _, _, anonymous) in Endpoints().Where(a => !a.anonymous))
            {
                var response = await client.SendAsync(Request(method, url));
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    failures.Add($"{method} {url}: {(int)response.StatusCode}");
                }
            }

            Assert.Empty(failures);
        }

        [Fact]
        public async Task EveryAdminEndpoint_WithVisuToken_Returns403()
        {
            var client = _host.CreateClient(_host.CreateVisuToken());
            var failures = new List<string>();
            var adminEndpoints = Endpoints().Where(a => !a.anonymous && a.authorize.Any(x =>
                    x.Policy == Role.AdminRole || (x.Roles ?? "").Split(',').Select(r => r.Trim()).Contains(Role.AdminRole)))
                // the visu actions let the visu role in on purpose (VisuActions_AreExactlyTheIntendedOnes)
                .Where(a => !IntendedVisuActions.Contains($"{a.action.ControllerTypeInfo.Name}.{a.action.MethodInfo.Name}"))
                .ToList();

            Assert.NotEmpty(adminEndpoints);
            foreach (var (method, url, _, _, _) in adminEndpoints)
            {
                var response = await client.SendAsync(Request(method, url));
                if (response.StatusCode != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{method} {url}: {(int)response.StatusCode}");
                }
            }

            Assert.Empty(failures);
        }

        [Fact]
        public async Task EveryProtectedEndpoint_WithPendingPasswordChangeToken_Returns403_ExceptThePasswordChange()
        {
            var userId = _host.CreateUser(_host.UniqueName("pending"), WebApiHost.StrongPassword, new[] { Role.AdminRole }, mustChangePassword: true);
            var client = _host.CreateClient(_host.TokenFor(userId));
            var failures = new List<string>();

            foreach (var (method, url, action, _, _) in Endpoints().Where(a => !a.anonymous))
            {
                if (action.MethodInfo.Name == nameof(AccountController.ChangePassword))
                {
                    continue;
                }

                var response = await client.SendAsync(Request(method, url));
                if (response.StatusCode != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{method} {url}: {(int)response.StatusCode}");
                }
            }

            Assert.Empty(failures);
        }

        [Theory]
        [InlineData("webapi/version")]
        [InlineData("webapi/localization")]
        [InlineData("webapi/localization/en")]
        [InlineData("webapi/settings/language")]
        public async Task AnonymousEndpoints_WorkWithoutToken(string url)
        {
            var response = await _host.CreateClient().GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("webapi/Ping")]
        [InlineData("webapi/server/state")]
        [InlineData("webapi/areas")]
        [InlineData("webapi/areas/templates")]
        [InlineData("webapi/categories")]
        [InlineData("webapi/categories/groups")]
        [InlineData("webapi/visualization/pages")]
        [InlineData("webapi/visualization/templates")]
        [InlineData("webapi/data/all/current")]
        [InlineData("webapi/data/node/current")]
        public async Task ViewerEndpoints_WorkWithVisuToken(string url)
        {
            var response = await _host.CreateClient(_host.CreateVisuToken()).GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ViewerPolicy_AcceptsTheViewerRole()
        {
            var userId = _host.CreateUser(_host.UniqueName("viewer"), WebApiHost.StrongPassword, new[] { Role.ViewerRole });
            var client = _host.CreateClient(_host.TokenFor(userId));

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("webapi/Ping")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("webapi/usermgm/users")).StatusCode);
        }

        [Fact]
        public async Task ViewerPolicy_RejectsAUserWithoutRoles()
        {
            var userId = _host.CreateUser(_host.UniqueName("norole"), WebApiHost.StrongPassword, Array.Empty<string>());
            var client = _host.CreateClient(_host.TokenFor(userId));

            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("webapi/Ping")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("webapi/areas")).StatusCode);
        }

        [Fact]
        public async Task AdminEndpoints_WorkWithAdminToken()
        {
            var client = _host.CreateClient(_host.CreateAdminToken());
            foreach (var url in new[] { "webapi/usermgm/users", "webapi/usermgm/roles", "webapi/settings", "webapi/satellite", "webapi/areas/all", "webapi/categories/all", "webapi/logics/pages" })
            {
                var response = await client.GetAsync(url);
                Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url}: {(int)response.StatusCode}");
            }
        }
    }
}
