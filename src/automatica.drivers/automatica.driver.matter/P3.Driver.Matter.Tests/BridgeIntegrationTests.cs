using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using P3.Driver.Matter.Certificates;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.DataModel.Clusters;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Storage;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Tests
{
    /// <summary>Commissions a real bridge over loopback UDP with <see cref="MiniController"/> and drives its devices.</summary>
    public sealed class BridgeIntegrationTests : IAsyncLifetime
    {
        private const ulong FabricId = 0x0000_0000_0000_0B0B;
        private const ulong ControllerNodeId = 0x0000_0000_0001_B669;
        private const ulong DeviceNodeId = 0x0000_0000_00AB_CDEF;

        private readonly InMemoryMatterStorage _storage = new();
        private MatterBridge _bridge;
        private BridgedOnOffDevice _light;
        private BridgedDimmableLight _dimmer;
        private BridgedWindowCovering _blind;
        private readonly List<bool> _switchCalls = new();
        private readonly List<int> _levelCalls = new();
        private readonly List<(CoveringCommand, int?)> _coverCalls = new();

        public async Task InitializeAsync()
        {
            _bridge = new MatterBridge(new MatterNodeOptions { Port = 0, EnableMdns = false, DeviceName = "Test Bridge" }, _storage);
            await _bridge.InitializeAsync();
            _light = _bridge.Add(new BridgedOnOffDevice("light-1", "Kitchen")
            {
                SwitchRequested = on =>
                {
                    lock (_switchCalls) _switchCalls.Add(on);
                    return Task.CompletedTask;
                }
            });
            _dimmer = _bridge.Add(new BridgedDimmableLight("dimmer-1", "Living room")
            {
                LevelRequested = percent =>
                {
                    lock (_levelCalls) _levelCalls.Add(percent);
                    return Task.CompletedTask;
                }
            });
            _blind = _bridge.Add(new BridgedWindowCovering("blind-1", "Bedroom")
            {
                CommandRequested = (command, percent) =>
                {
                    lock (_coverCalls) _coverCalls.Add((command, percent));
                    return Task.CompletedTask;
                }
            });
            await _bridge.StartAsync();
        }

        public async Task DisposeAsync() => await _bridge.DisposeAsync();

        [Fact]
        public async Task Wrong_passcode_is_rejected()
        {
            using var controller = new MiniController(_bridge.Node.Port);
            var wrong = _bridge.Node.State.Passcode == 20202021 ? 11223344u : 20202021u;
            var status = await controller.FailPaseAsync(wrong);
            Assert.False(status.IsSuccess);
            Assert.Null(controller.Session);
        }

        [Fact]
        public async Task Commission_then_control_over_case()
        {
            using var controller = new MiniController(_bridge.Node.Port);
            using var ca = new TestCertificateAuthority(FabricId);
            Assert.True(_bridge.Node.IsCommissioningWindowOpen);

            // ---- PASE
            var pase = await controller.EstablishPaseAsync(_bridge.Node.State.Passcode);
            Assert.NotNull(pase);

            // ---- fail-safe
            var arm = await controller.InvokeWithResponseAsync(0, GeneralCommissioningCluster.Id, 0x00, w =>
            {
                w.PutUInt(0, 60);
                w.PutUInt(1, 1);
            });
            Assert.Equal(0ul, arm.GetUInt(0));

            var regulatory = await controller.InvokeWithResponseAsync(0, GeneralCommissioningCluster.Id, 0x02, w =>
            {
                w.PutUInt(0, 2);
                w.PutString(1, "AT");
                w.PutUInt(2, 2);
            });
            Assert.Equal(0ul, regulatory.GetUInt(0));

            // ---- device attestation
            var dacDer = (await controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x02, w => w.PutUInt(0, 1))).GetBytes(0);
            var paiDer = (await controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x02, w => w.PutUInt(0, 2))).GetBytes(0);
            using var dac = X509CertificateLoader.LoadCertificate(dacDer);
            using var pai = X509CertificateLoader.LoadCertificate(paiDer);
            AttestationTests.AssertChainsToTestPaa(dac, pai);

            var attestationNonce = MatterCrypto.RandomBytes(32);
            var attestation = await controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x00, w => w.PutBytes(0, attestationNonce));
            var attestationElements = attestation.GetBytes(0);
            using (var dacKey = dac.GetECDsaPublicKey()!)
            {
                Assert.True(dacKey.VerifyData(attestationElements.Concat(pase.AttestationChallenge).ToArray(), attestation.GetBytes(1), HashAlgorithmName.SHA256),
                    "attestation signature");
            }

            var elements = TlvReader.Decode(attestationElements);
            Assert.Equal(attestationNonce, elements.GetBytes(2));
            AttestationTests.AssertValidCertificationDeclaration(elements.GetBytes(1));

            // ---- CSR
            var csrNonce = MatterCrypto.RandomBytes(32);
            var csrResponse = await controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x04, w => w.PutBytes(0, csrNonce));
            var nocsr = TlvReader.Decode(csrResponse.GetBytes(0));
            Assert.Equal(csrNonce, nocsr.GetBytes(2));
            var request = CertificateRequest.LoadSigningRequest(nocsr.GetBytes(1), HashAlgorithmName.SHA256, CertificateRequestLoadOptions.Default);
            using var csrKey = request.PublicKey.GetECDsaPublicKey()!;
            var deviceOperationalKey = MatterCrypto.ExportPublicKey(csrKey);

            // ---- operational credentials
            var root = await controller.InvokeAsync(0, OperationalCredentialsCluster.Id, 0x0B, w => w.PutBytes(0, ca.RootCert));
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(root));

            var ipkEpoch = MatterCrypto.RandomBytes(16);
            var deviceNoc = ca.IssueNoc(deviceOperationalKey, DeviceNodeId);
            var noc = await controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x06, w =>
            {
                w.PutBytes(0, deviceNoc);
                w.PutBytes(2, ipkEpoch);
                w.PutUInt(3, ControllerNodeId);
                w.PutUInt(4, DeviceAttestation.TestVendorId);
            });
            Assert.Equal((ulong)NocStatus.Ok, noc.GetUInt(0));
            var fabricIndex = (byte)noc.GetUInt(1);

            // ---- CASE
            using var controllerKey = MatterCrypto.CreateSigningKey();
            var controllerNoc = ca.IssueNoc(MatterCrypto.ExportPublicKey(controllerKey), ControllerNodeId);
            var caseSession = await controller.EstablishCaseAsync(ca, ipkEpoch, ControllerNodeId, controllerKey, controllerNoc, DeviceNodeId);
            Assert.NotNull(caseSession);

            var complete = await controller.InvokeWithResponseAsync(0, GeneralCommissioningCluster.Id, 0x04);
            Assert.Equal(0ul, complete.GetUInt(0));
            Assert.Equal(1, _bridge.Node.CommissionedFabricCount);
            Assert.False(_bridge.Node.IsCommissioningWindowOpen);

            // ---- structure
            var parts = (await controller.ReadAsync((0, DescriptorCluster.Id, DescriptorCluster.AttributePartsList)))
                .Data(0, DescriptorCluster.Id, DescriptorCluster.AttributePartsList);
            var endpoints = parts.Children.Select(c => (ushort)c.AsUInt()).ToList();
            Assert.Contains(MatterBridge.AggregatorEndpoint, endpoints);
            Assert.Contains(_light.EndpointId, endpoints);
            Assert.Contains(_dimmer.EndpointId, endpoints);
            Assert.Contains(_blind.EndpointId, endpoints);

            var wildcard = await controller.ReadAsync((_light.EndpointId, null, null));
            Assert.NotNull(wildcard.Data(_light.EndpointId, OnOffCluster.Id, 0x0000));
            Assert.Equal("Kitchen", wildcard.Data(_light.EndpointId, BridgedDeviceBasicInformationCluster.Id, 0x0005).AsString());

            var fabrics = (await controller.ReadAsync((0, OperationalCredentialsCluster.Id, 0x0001))).Data(0, OperationalCredentialsCluster.Id, 0x0001);
            Assert.Single(fabrics.Children);
            Assert.Equal(FabricId, fabrics.Children[0].GetUInt(3));
            Assert.Equal(DeviceNodeId, fabrics.Children[0].GetUInt(4));

            // ---- commands
            var toggle = await controller.InvokeAsync(_light.EndpointId, OnOffCluster.Id, 0x02);
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(toggle));
            lock (_switchCalls) Assert.Equal(new[] { true }, _switchCalls);

            _dimmer.SetState(true, 20);
            var level = await controller.InvokeAsync(_dimmer.EndpointId, LevelControlCluster.Id, 0x00, w =>
            {
                w.PutUInt(0, 254);
                w.PutUInt(1, 0);
                w.PutUInt(2, 0);
                w.PutUInt(3, 0);
            });
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(level));
            lock (_levelCalls) Assert.Equal(new[] { 100 }, _levelCalls);

            var lift = await controller.InvokeAsync(_blind.EndpointId, WindowCoveringCluster.Id, 0x05, w => w.PutUInt(0, 5000));
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(lift));
            lock (_coverCalls) Assert.Equal(new (CoveringCommand, int?)[] { (CoveringCommand.GoToPosition, 50) }, _coverCalls);

            // ---- subscription
            var subscribeExchange = controller.NewExchange();
            await controller.SendAsync(subscribeExchange, true, ProtocolId.InteractionModel, ImOpcode.SubscribeRequest, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBool(0, false);
                w.PutUInt(1, 0);
                w.PutUInt(2, 10);
                w.StartArray(3);
                w.StartList();
                w.PutUInt(2, _light.EndpointId);
                w.PutUInt(3, OnOffCluster.Id);
                w.PutUInt(4, 0);
                w.EndContainer();
                w.EndContainer();
                w.PutBool(7, true);
                w.PutUInt(0xFF, 11);
                w.EndContainer();
            }));
            var priming = await controller.CollectReportsAsync(subscribeExchange, false);
            Assert.NotNull(priming.Data(_light.EndpointId, OnOffCluster.Id, 0));
            var subscribeResponse = await controller.NextAsync(m => m.PayloadHeader.ExchangeId == subscribeExchange && m.Opcode == ImOpcode.SubscribeResponse);
            var subscriptionId = TlvReader.Decode(subscribeResponse.Payload).GetUInt(0);

            var target = !_light.IsOn;
            _light.SetOnOff(target);
            var report = await controller.NextAsync(m => m.Opcode == ImOpcode.ReportData && m.PayloadHeader.IsInitiator);
            var reportData = TlvReader.Decode(report.Payload);
            Assert.Equal(subscriptionId, reportData.GetUInt(0));
            Assert.Equal(target, reportData.Require(1).Children.Data(_light.EndpointId, OnOffCluster.Id, 0).AsBool());
            await controller.SendStatusResponseAsync(report.PayloadHeader.ExchangeId, false);

            // ---- persistence: a node loaded from the same storage knows the fabric
            await _bridge.Node.SaveAsync();
            var reloaded = MatterState.Deserialize(_storage.Json);
            Assert.Single(reloaded.Fabrics);

            // ---- leave the fabric
            var remove = await controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x0A, w => w.PutUInt(0, fabricIndex));
            Assert.Equal((ulong)NocStatus.Ok, remove.GetUInt(0));
            Assert.Equal(0, _bridge.Node.CommissionedFabricCount);
        }
    }
}
