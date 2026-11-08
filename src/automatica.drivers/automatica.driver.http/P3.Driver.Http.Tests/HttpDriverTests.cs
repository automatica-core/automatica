using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Http.Driver;
using P3.Driver.Http.Driver.Extraction;
using Xunit;

namespace P3.Driver.Http.Tests
{
    public class HttpDriverTests : DriverFactoryTestBase<HttpDriverFactory>
    {
        private sealed class FakeHandler : HttpMessageHandler
        {
            public List<(HttpMethod Method, Uri Uri, string Authorization, string Body, string ContentType)> Requests { get; } = new();

            public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
                _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request.Method, request.RequestUri, request.Headers.Authorization?.ToString(), body, request.Content?.Headers.ContentType?.ToString()));
                return Respond(request);
            }
        }

        private NodeInstance Child(NodeInstance parent, Guid template)
        {
            var node = CreateNodeInstance(template);
            parent.InverseThis2ParentNodeInstanceNavigation.Add(node);
            return node;
        }

        private NodeInstance Input(NodeInstance root, string url, string user = "", string password = "")
        {
            var input = Child(root, HttpDriverFactory.InputTemplate);
            input.SetProperty(HttpDriverFactory.UrlKey, url);
            input.SetProperty(HttpDriverFactory.PollKey, 60);
            input.SetProperty(HttpDriverFactory.TimeoutKey, 5);
            input.SetProperty(HttpDriverFactory.UserKey, user);
            input.SetProperty(HttpDriverFactory.PasswordKey, password);
            return input;
        }

        private NodeInstance InputCommand(NodeInstance input, Guid template, ExtractMode mode, string pattern)
        {
            var command = Child(input, template);
            command.SetProperty(HttpDriverFactory.ModeKey, (int)mode);
            command.SetProperty(HttpDriverFactory.PatternKey, pattern);
            return command;
        }

        private NodeInstance Output(NodeInstance root, string address, string user = "", string password = "")
        {
            var output = Child(root, HttpDriverFactory.OutputTemplate);
            output.SetProperty(HttpDriverFactory.AddressKey, address);
            output.SetProperty(HttpDriverFactory.TimeoutKey, 5);
            output.SetProperty(HttpDriverFactory.UserKey, user);
            output.SetProperty(HttpDriverFactory.PasswordKey, password);
            return output;
        }

        private NodeInstance OutputCommand(NodeInstance output, Guid template, CommandMethod method, string on, string off = "", string bodyOn = "", string bodyOff = "")
        {
            var command = Child(output, template);
            command.SetProperty(HttpDriverFactory.MethodKey, (int)method);
            command.SetProperty(HttpDriverFactory.CommandOnKey, on);
            command.SetProperty(HttpDriverFactory.CommandOffKey, off);
            command.SetProperty(HttpDriverFactory.BodyOnKey, bodyOn);
            command.SetProperty(HttpDriverFactory.BodyOffKey, bodyOff);
            command.SetProperty(HttpDriverFactory.ContentTypeKey, HttpDriverFactory.DefaultContentType);
            return command;
        }

        private NodeInstance Root(bool allowLocalHost = false)
        {
            var root = CreateNodeInstance(DriverFactory.DriverGuid);
            root.SetProperty(HttpDriverFactory.AllowLocalHostKey, allowLocalHost);
            return root;
        }

        private async Task<(HttpDriver Driver, FakeHandler Handler)> StartAsync(NodeInstance root, bool fake = true)
        {
            var driver = (HttpDriver)await CreateDriver(root);
            var handler = new FakeHandler();
            if (fake)
            {
                driver.HandlerFactory = () => handler;
            }
            return (driver, handler);
        }

        private static T Node<T>(Automatica.Core.Driver.IDriverNode parent, int index = 0) => parent.Children.OfType<T>().ElementAt(index);

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        [Fact]
        public async Task Input_Poll_ExtractsEveryCommand()
        {
            var root = Root();
            var input = Input(root, "http://192.168.1.20/status");
            var online = Child(input, HttpDriverFactory.OnlineTemplate);
            var temp = InputCommand(input, HttpDriverFactory.InputValueTemplate, ExtractMode.Json, "$.temp");
            var relay = InputCommand(input, HttpDriverFactory.InputDigitalTemplate, ExtractMode.Json, "$.relays[0].ison");
            var name = InputCommand(input, HttpDriverFactory.InputTextTemplate, ExtractMode.Regex, "\"name\":\"([^\"]+)\"");
            var (driver, handler) = await StartAsync(root);
            handler.Respond = _ => Ok("{\"temp\":21.5,\"relays\":[{\"ison\":true}],\"name\":\"Kitchen\"}");

            await Node<InputDeviceNode>(driver).PollAsync();

            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://192.168.1.20/status", request.Uri.ToString());
            Assert.Null(request.Authorization);
            Assert.Equal(true, Value(online));
            Assert.Equal(21.5, Value(temp));
            Assert.Equal(true, Value(relay));
            Assert.Equal("Kitchen", Value(name));
        }

        [Fact]
        public async Task Input_BasicAuth_IsSentWhenAUserIsSet()
        {
            var root = Root();
            var input = Input(root, "https://192.168.1.20/status", "admin", "s3cret");
            InputCommand(input, HttpDriverFactory.InputValueTemplate, ExtractMode.Text, "");
            var (driver, handler) = await StartAsync(root);
            handler.Respond = _ => Ok("1");

            await Node<InputDeviceNode>(driver).PollAsync();

            Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:s3cret")), handler.Requests.Single().Authorization);
        }

        [Fact]
        public async Task Input_ErrorStatus_IsOffline_AndKeepsTheValues()
        {
            var root = Root();
            var input = Input(root, "http://192.168.1.20/status");
            var online = Child(input, HttpDriverFactory.OnlineTemplate);
            var temp = InputCommand(input, HttpDriverFactory.InputValueTemplate, ExtractMode.Json, "$.temp");
            var (driver, handler) = await StartAsync(root);
            var device = Node<InputDeviceNode>(driver);

            handler.Respond = _ => Ok("{\"temp\":20}");
            await device.PollAsync();
            handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);
            await device.PollAsync();

            Assert.Equal(false, Value(online));
            Assert.Equal(20.0, Value(temp));
        }

        [Fact]
        public async Task Input_ValueNotFound_KeepsTheLastValue()
        {
            var root = Root();
            var input = Input(root, "http://192.168.1.20/status");
            var temp = InputCommand(input, HttpDriverFactory.InputValueTemplate, ExtractMode.Pattern, "\\iTemp=\\i\\v");
            var (driver, handler) = await StartAsync(root);
            var device = Node<InputDeviceNode>(driver);

            handler.Respond = _ => Ok("Temp=4.5");
            await device.PollAsync();
            handler.Respond = _ => Ok("Temp=error");
            await device.PollAsync();

            Assert.Equal(4.5, Value(temp));
            Assert.NotNull(device.Commands.Single().Error);
        }

        [Fact]
        public async Task Input_InvalidUrl_IsNotPolled()
        {
            var root = Root();
            Input(root, "file:///etc/passwd");
            var (driver, handler) = await StartAsync(root);

            await Node<InputDeviceNode>(driver).PollAsync();

            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task Output_Command_ReplacesTheValue()
        {
            var root = Root();
            var output = Output(root, "http://192.168.1.30");
            OutputCommand(output, HttpDriverFactory.OutputValueTemplate, CommandMethod.Get, "/light/0?brightness=<v>&t=<v.1>");
            var (driver, handler) = await StartAsync(root);
            var command = Node<OutputCommandNode>(Node<OutputDeviceNode>(driver));

            await command.SendAsync(42.26);
            await command.SendAsync(0);

            Assert.Equal(new[] { "http://192.168.1.30/light/0?brightness=42.26&t=42.3", "http://192.168.1.30/light/0?brightness=0&t=0.0" },
                handler.Requests.Select(r => r.Uri.ToString()));
        }

        [Fact]
        public async Task Output_DigitalCommand_OnAndOff()
        {
            var root = Root();
            var output = Output(root, "http://192.168.1.30/");
            var status = Child(output, HttpDriverFactory.StatusTemplate);
            OutputCommand(output, HttpDriverFactory.OutputDigitalTemplate, CommandMethod.Get, "relay/0?turn=on", "relay/0?turn=off");
            var (driver, handler) = await StartAsync(root);
            var command = Node<OutputCommandNode>(Node<OutputDeviceNode>(driver));

            await command.SendAsync(true);
            await command.SendAsync(false);

            Assert.Equal(new[] { "http://192.168.1.30/relay/0?turn=on", "http://192.168.1.30/relay/0?turn=off" }, handler.Requests.Select(r => r.Uri.ToString()));
            Assert.Equal(200, Value(status));
        }

        [Fact]
        public async Task Output_DigitalCommandWithoutOff_SendsNothingForFalse()
        {
            var root = Root();
            var output = Output(root, "http://192.168.1.30");
            OutputCommand(output, HttpDriverFactory.OutputDigitalTemplate, CommandMethod.Get, "/bell");
            var (driver, handler) = await StartAsync(root);
            var command = Node<OutputCommandNode>(Node<OutputDeviceNode>(driver));

            Assert.Null(await command.SendAsync(false));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task Output_Post_SendsTheBodyAndAuth()
        {
            var root = Root();
            var output = Output(root, "https://192.168.1.30", "user", "pw");
            OutputCommand(output, HttpDriverFactory.OutputDigitalTemplate, CommandMethod.Post, "/api/switch", bodyOn: "{\"on\":<v>}", off: "/api/switch", bodyOff: "{\"on\":false}");
            var (driver, handler) = await StartAsync(root);
            var command = Node<OutputCommandNode>(Node<OutputDeviceNode>(driver));

            await command.SendAsync(true);
            await command.SendAsync(false);

            Assert.Equal(new[] { "{\"on\":1}", "{\"on\":false}" }, handler.Requests.Select(r => r.Body));
            Assert.All(handler.Requests, r =>
            {
                Assert.Equal(HttpMethod.Post, r.Method);
                Assert.StartsWith("application/json", r.ContentType);
                Assert.StartsWith("Basic ", r.Authorization);
            });
        }

        [Fact]
        public async Task Output_FailedRequest_StatusIsZero()
        {
            var root = Root();
            var output = Output(root, "http://192.168.1.30");
            var status = Child(output, HttpDriverFactory.StatusTemplate);
            OutputCommand(output, HttpDriverFactory.OutputValueTemplate, CommandMethod.Get, "/x?v=<v>");
            var (driver, handler) = await StartAsync(root);
            handler.Respond = _ => throw new HttpRequestException("connection refused");
            var command = Node<OutputCommandNode>(Node<OutputDeviceNode>(driver));

            var result = await command.SendAsync(1);

            Assert.False(result!.Value.Success);
            Assert.Equal(0, Value(status));
        }

        [Fact]
        public async Task EndToEnd_LoopbackIsBlockedByDefault()
        {
            await using var server = await LoopbackServer.StartAsync("HTTP/1.1 200 OK\r\nContent-Length: 11\r\nConnection: close\r\n\r\n{\"temp\":19}");
            var root = Root(allowLocalHost: false);
            var input = Input(root, server.Url);
            var online = Child(input, HttpDriverFactory.OnlineTemplate);
            var (driver, _) = await StartAsync(root, fake: false);

            await Node<InputDeviceNode>(driver).PollAsync();

            Assert.Equal(false, Value(online));
            Assert.Equal(0, server.Requests);
            Assert.Contains("blocked", Node<InputDeviceNode>(driver).Error);
            await driver.Stop();
        }

        [Fact]
        public async Task EndToEnd_LoopbackAllowed_ReadsTheValue()
        {
            await using var server = await LoopbackServer.StartAsync("HTTP/1.1 200 OK\r\nContent-Length: 11\r\nConnection: close\r\n\r\n{\"temp\":19}");
            var root = Root(allowLocalHost: true);
            var input = Input(root, server.Url, "admin", "pw");
            var temp = InputCommand(input, HttpDriverFactory.InputValueTemplate, ExtractMode.Json, "temp");
            var (driver, _) = await StartAsync(root, fake: false);

            await Node<InputDeviceNode>(driver).PollAsync();

            Assert.Equal(19.0, Value(temp));
            Assert.Contains("Authorization: Basic ", server.LastRequest);
            await driver.Stop();
        }
    }
}
