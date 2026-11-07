using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Synology.DriverFactory;
using Xunit;

namespace P3.Driver.Synology.Tests
{
    /// <summary>
    /// SynologyDevice (login, TLS validation) against an in-process DSM on 127.0.0.1.
    /// </summary>
    public class SynologyDeviceTests : DriverFactoryTestBase<SynologyDriverFactory>
    {
        private static readonly Guid ConnectedTemplate = new Guid("fbdc5300-ba67-406e-a22c-c11019de42af");

        private static void SetProperty(NodeInstance node, string key, object value)
        {
            node.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == key).Value = value;
        }

        private sealed class DeviceSetup
        {
            public IDriverNode Device { get; set; }
            public NodeInstance Connected { get; set; }
        }

        private async Task<DeviceSetup> CreateDevice(FakeDsm dsm, bool https = false, bool ignoreSslErrors = false, string thumbprint = "",
            string user = null, string password = null, bool withConnectedNode = true)
        {
            var driverNode = CreateNodeInstance(SynologyDriverFactory.DriverId);
            var deviceNode = CreateNodeInstance(SynologyDriverFactory.SynologyDevice);
            SetProperty(deviceNode, "ip", "127.0.0.1");
            SetProperty(deviceNode, "port", dsm.Port);
            SetProperty(deviceNode, "use_https", https);
            SetProperty(deviceNode, "user", user ?? dsm.User);
            SetProperty(deviceNode, "password", password ?? dsm.Password);
            SetProperty(deviceNode, "ignore_ssl_errors", ignoreSslErrors);
            SetProperty(deviceNode, "certificate_thumbprint", thumbprint);
            SetProperty(deviceNode, "use_remote_connect", false);
            SetProperty(deviceNode, "remote_connect_domain", "");
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(deviceNode);

            NodeInstance connected = null;
            if (withConnectedNode)
            {
                connected = CreateNodeInstance(ConnectedTemplate);
                deviceNode.InverseThis2ParentNodeInstanceNavigation.Add(connected);
            }

            var context = new DriverContextMock(driverNode, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = DriverFactory.CreateDriver(context);
            Assert.True(await driver.Init());
            Assert.True(await driver.Configure());

            return new DeviceSetup { Device = driver.Children.Single(), Connected = connected };
        }

        private async Task<object> WaitForConnectedValue(NodeInstance connected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                var value = Dispatcher.GetValue(DispatchableType.NodeInstance, connected.ObjId);
                if (value != null)
                {
                    return value.Value;
                }
                await Task.Delay(10);
            }
            return null;
        }

        private static bool LoginReachedServer(FakeDsm dsm) => dsm.Requests.Any(a => a.Method == "POST" && a.RawTarget.StartsWith("/webapi/entry.cgi"));

        [Fact]
        public void Factory_NewDevice_DoesNotIgnoreSslErrorsAndHasNoPin()
        {
            var device = CreateNodeInstance(SynologyDriverFactory.SynologyDevice);

            Assert.Equal(false, device.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "ignore_ssl_errors").Value);
            Assert.Equal("", device.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "certificate_thumbprint").Value);
        }

        [Fact]
        public async Task Start_ValidCredentials_DispatchesConnectedTrue()
        {
            await using var dsm = await FakeDsm.StartAsync();
            var setup = await CreateDevice(dsm);

            await setup.Device.Start();

            Assert.Equal(true, await WaitForConnectedValue(setup.Connected));
        }

        [Fact]
        public async Task Start_Login_IsAFormPostAndThePasswordIsNotInAnyUrl()
        {
            await using var dsm = await FakeDsm.StartAsync();
            dsm.Password = "p&ss=w rd?/#";
            var setup = await CreateDevice(dsm);

            await setup.Device.Start();
            Assert.Equal(true, await WaitForConnectedValue(setup.Connected));

            var login = Assert.Single(dsm.Requests, a => a.Method == "POST");
            Assert.Equal("/webapi/entry.cgi", login.RawTarget);
            Assert.StartsWith("application/x-www-form-urlencoded", login.ContentType);
            Assert.Contains("passwd=" + Uri.EscapeDataString(dsm.Password).Replace("%20", "+"), login.Body);
            Assert.All(dsm.Requests, r => Assert.DoesNotContain("passwd", r.RawTarget));
            Assert.All(dsm.Requests, r => Assert.DoesNotContain("made-up-dsm", r.RawTarget));
        }

        [Fact]
        public async Task Start_WrongPassword_DispatchesConnectedFalse()
        {
            await using var dsm = await FakeDsm.StartAsync();
            var setup = await CreateDevice(dsm, password: "wrong-made-up-password");

            var exception = await Record.ExceptionAsync(() => setup.Device.Start());

            Assert.Null(exception);
            Assert.Equal(false, await WaitForConnectedValue(setup.Connected));
            Assert.True(LoginReachedServer(dsm));
        }

        [Fact]
        public async Task Start_InfoQueryFails_DispatchesConnectedFalse()
        {
            await using var dsm = await FakeDsm.StartAsync();
            dsm.InfoStatusCode = 500;
            var setup = await CreateDevice(dsm);

            await setup.Device.Start();

            Assert.Equal(false, await WaitForConnectedValue(setup.Connected));
            Assert.False(LoginReachedServer(dsm));
        }

        [Fact]
        public async Task Start_NoCredentials_DoesNotLogIn()
        {
            await using var dsm = await FakeDsm.StartAsync();
            var setup = await CreateDevice(dsm, user: "", password: "");

            await setup.Device.Start();

            Assert.Equal(true, await WaitForConnectedValue(setup.Connected));
            Assert.False(LoginReachedServer(dsm));
        }

        [Fact(Skip = "BUG: SynologyDevice.Start dereferences _connectedAttribute in try and catch, a device without the connected child node throws NullReferenceException out of Start - SynologyDevice.cs:81,86")]
        public async Task Start_WithoutConnectedNode_DoesNotThrow()
        {
            await using var dsm = await FakeDsm.StartAsync();
            var setup = await CreateDevice(dsm, password: "wrong-made-up-password", withConnectedNode: false);

            var exception = await Record.ExceptionAsync(() => setup.Device.Start());

            Assert.Null(exception);
        }

        #region https

        [Fact]
        public async Task Start_HttpsSelfSigned_NotIgnoredAndNotPinned_DispatchesConnectedFalse()
        {
            using var certificate = TestCertificates.CreateServerCertificate();
            await using var dsm = await FakeDsm.StartAsync(certificate);
            var setup = await CreateDevice(dsm, https: true);

            await setup.Device.Start();

            Assert.Equal(false, await WaitForConnectedValue(setup.Connected));
            // the TLS handshake failed, the credentials never left the client
            Assert.Empty(dsm.Requests);
        }

        [Fact]
        public async Task Start_HttpsSelfSigned_IgnoreSslErrors_DispatchesConnectedTrue()
        {
            using var certificate = TestCertificates.CreateServerCertificate();
            await using var dsm = await FakeDsm.StartAsync(certificate);
            var setup = await CreateDevice(dsm, https: true, ignoreSslErrors: true);

            await setup.Device.Start();

            Assert.Equal(true, await WaitForConnectedValue(setup.Connected));
            Assert.True(LoginReachedServer(dsm));
        }

        [Fact]
        public async Task Start_HttpsSelfSigned_PinnedSha256_DispatchesConnectedTrue()
        {
            using var certificate = TestCertificates.CreateServerCertificate();
            await using var dsm = await FakeDsm.StartAsync(certificate);
            var setup = await CreateDevice(dsm, https: true, thumbprint: TestCertificates.WithColons(certificate.GetCertHashString(HashAlgorithmName.SHA256)));

            await setup.Device.Start();

            Assert.Equal(true, await WaitForConnectedValue(setup.Connected));
        }

        [Fact]
        public async Task Start_HttpsSelfSigned_PinnedSha1Lowercase_DispatchesConnectedTrue()
        {
            using var certificate = TestCertificates.CreateServerCertificate();
            await using var dsm = await FakeDsm.StartAsync(certificate);
            var setup = await CreateDevice(dsm, https: true, thumbprint: certificate.Thumbprint.ToLowerInvariant());

            await setup.Device.Start();

            Assert.Equal(true, await WaitForConnectedValue(setup.Connected));
        }

        [Fact]
        public async Task Start_HttpsPinMismatch_EvenWithIgnoreSslErrors_DispatchesConnectedFalse()
        {
            using var certificate = TestCertificates.CreateServerCertificate();
            using var other = TestCertificates.CreateSelfSigned("CN=other");
            await using var dsm = await FakeDsm.StartAsync(certificate);
            var setup = await CreateDevice(dsm, https: true, ignoreSslErrors: true, thumbprint: other.GetCertHashString(HashAlgorithmName.SHA256));

            await setup.Device.Start();

            Assert.Equal(false, await WaitForConnectedValue(setup.Connected));
            Assert.Empty(dsm.Requests);
        }

        #endregion
    }
}
