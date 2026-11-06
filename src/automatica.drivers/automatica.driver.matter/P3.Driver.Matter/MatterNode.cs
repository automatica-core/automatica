using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Matter.Certificates;
using P3.Driver.Matter.Commissioning;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.DataModel;
using P3.Driver.Matter.DataModel.Clusters;
using P3.Driver.Matter.Discovery;
using P3.Driver.Matter.Exchanges;
using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.SecureChannel;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Storage;
using P3.Driver.Matter.Transport;

namespace P3.Driver.Matter
{
    public sealed class MatterNodeOptions
    {
        public int Port { get; set; } = UdpTransport.DefaultPort;
        public string VendorName { get; set; } = "P3 Software";
        public string ProductName { get; set; } = "Automatica Matter Bridge";
        public string DeviceName { get; set; } = "Automatica Bridge";
        public ushort HardwareVersion { get; set; } = 1;
        public string HardwareVersionString { get; set; } = "1.0";
        public uint SoftwareVersion { get; set; } = 1;
        public string SoftwareVersionString { get; set; } = "1.0.0";
        public string SerialNumber { get; set; }
        public uint PrimaryDeviceType { get; set; } = DeviceType.Aggregator.Id;
        public bool EnableMdns { get; set; } = true;

        /// <summary>Attestation credentials; the CSA test PKI (VID 0xFFF1 / PID 0x8000) when null.</summary>
        public DeviceAttestation Attestation { get; set; }
    }

    /// <summary>
    /// A Matter node: owns the transport, sessions, the commissioning state machine (fail-safe, fabrics, windows)
    /// and the data model with the root endpoint. <see cref="MatterBridge"/> adds the bridged devices on top.
    /// </summary>
    public sealed class MatterNode : IPaseContext, IAsyncDisposable
    {
        public const byte SupportedFabrics = 5;
        private const int MaxPaseAttempts = 20;

        private readonly IMatterStorage _storage;
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private readonly SemaphoreSlim _persistGate = new(1, 1);
        private readonly List<Fabric> _fabrics = new();

        private UdpTransport _transport;
        private SessionManager _sessions;
        private ExchangeManager _exchanges;
        private PaseResponder _pase;
        private CaseResponder _case;
        private InteractionModelServer _im;
        private MdnsAdvertiser _mdns;
        private PasscodeVerifier _defaultVerifier;
        private FailSafeContext _failSafe;
        private CommissioningWindow _window;
        private bool _started;

        private sealed class FailSafeContext
        {
            public byte FabricIndex;
            public CancellationTokenSource Timer;
            public byte[] PendingRoot;
            public ECDsa PendingKey;
            public bool KeyForUpdate;
            public bool NocAdded;
            public Fabric AddedFabric;
            public (Fabric Fabric, byte[] Noc, byte[] Icac, ECDsa Key)? UpdateBackup;
        }

        private sealed class CommissioningWindow
        {
            public PasscodeVerifier Verifier;
            public byte Mode;
            public ushort Discriminator;
            public byte? AdminFabricIndex;
            public ushort? AdminVendorId;
            public CancellationTokenSource Timer;
            public int FailedAttempts;
        }

        public MatterNode(MatterNodeOptions options, IMatterStorage storage, ILogger logger = null)
        {
            Options = options ?? new MatterNodeOptions();
            _storage = storage ?? new InMemoryMatterStorage();
            _logger = logger ?? NullLogger.Instance;
            Attestation = Options.Attestation ?? DeviceAttestation.CreateTestCredentials();
            DataModel = new DataModelNode();
        }

        public MatterNodeOptions Options { get; }
        public DeviceAttestation Attestation { get; }
        public DataModelNode DataModel { get; }
        public MatterState State { get; private set; }
        public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
        public ulong Breadcrumb { get; internal set; }
        public int Port => _transport?.Port ?? Options.Port;

        /// <summary>Raised when fabrics or the commissioning window change.</summary>
        public event EventHandler StatusChanged;

        public string ManualPairingCode => SetupPayload.ManualPairingCode(State.Passcode, State.Discriminator);

        public string QrCodePayload => SetupPayload.QrCode(Attestation.VendorId, Attestation.ProductId, State.Passcode, State.Discriminator);

        public bool IsCommissioningWindowOpen
        {
            get
            {
                lock (_lock)
                {
                    return _window != null;
                }
            }
        }

        /// <summary>Committed fabrics plus the one being commissioned.</summary>
        public IReadOnlyList<Fabric> Fabrics
        {
            get
            {
                lock (_lock)
                {
                    var all = _fabrics.ToList();
                    if (_failSafe?.AddedFabric != null)
                    {
                        all.Add(_failSafe.AddedFabric);
                    }

                    return all.OrderBy(f => f.FabricIndex).ToList();
                }
            }
        }

        public int CommissionedFabricCount
        {
            get
            {
                lock (_lock)
                {
                    return _fabrics.Count;
                }
            }
        }

        internal IEnumerable<byte[]> TrustedRoots
        {
            get
            {
                lock (_lock)
                {
                    var roots = Fabrics.Select(f => f.RootCert).ToList();
                    if (_failSafe?.PendingRoot != null && _failSafe.AddedFabric == null)
                    {
                        roots.Add(_failSafe.PendingRoot);
                    }

                    return roots;
                }
            }
        }

        internal InteractionModelServer ImServer => _im;

        // ---------------------------------------------------------------- lifecycle

        /// <summary>Loads (or creates) the persistent state and builds the root endpoint.</summary>
        public async Task LoadAsync(CancellationToken token = default)
        {
            if (State != null)
            {
                return;
            }

            var state = MatterState.Deserialize(await _storage.LoadAsync(token));
            if (state == null || !SetupPayload.IsValidPasscode(state.Passcode))
            {
                state = MatterState.CreateNew();
            }

            state.RebootCount = unchecked((ushort)(state.RebootCount + 1));
            State = state;
            foreach (var stored in state.Fabrics)
            {
                try
                {
                    _fabrics.Add(stored.ToFabric());
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not restore Matter fabric {Index}", stored.FabricIndex);
                }
            }

            _defaultVerifier = PasscodeVerifier.FromPasscode(state.Passcode, Convert.FromBase64String(state.Salt), state.Iterations);

            var root = new Endpoint(0, null, DeviceType.RootNode);
            root.Add(new DescriptorCluster())
                .Add(new BasicInformationCluster(this))
                .Add(new AccessControlCluster(this))
                .Add(new GeneralCommissioningCluster(this))
                .Add(new NetworkCommissioningCluster())
                .Add(new GeneralDiagnosticsCluster(this))
                .Add(new AdministratorCommissioningCluster(this))
                .Add(new OperationalCredentialsCluster(this))
                .Add(new GroupKeyManagementCluster());
            DataModel.AddEndpoint(root);

            await PersistAsync();
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            await LoadAsync(token);
            if (_started)
            {
                return;
            }

            StartedAt = DateTime.UtcNow;
            _transport = new UdpTransport(_logger);
            _sessions = new SessionManager();
            _exchanges = new ExchangeManager(_transport, _sessions, _logger);
            _pase = new PaseResponder(_sessions, this, _logger);
            _case = new CaseResponder(_sessions, () => Fabrics, _logger);
            _im = new InteractionModelServer(DataModel, _exchanges, _sessions, _logger);
            _exchanges.RegisterProtocol(ProtocolId.SecureChannel, HandleSecureChannelAsync);
            _exchanges.RegisterProtocol(ProtocolId.InteractionModel, _im.HandleAsync);
            _transport.Start(Options.Port);

            if (Options.EnableMdns)
            {
                _mdns = new MdnsAdvertiser(_logger);
                try
                {
                    _mdns.Start(_transport.Port);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not start mDNS, controllers will not discover the bridge");
                    _mdns.Dispose();
                    _mdns = null;
                }
            }

            _started = true;
            UpdateOperationalAdvertisements();
            if (CommissionedFabricCount == 0)
            {
                OpenCommissioningWindow();
            }

            _logger.LogInformation("Matter node listening on UDP {Port}, {Fabrics} fabric(s), pairing code {Code}", _transport.Port, CommissionedFabricCount,
                SetupPayload.FormatManualCode(ManualPairingCode));
        }

        public async ValueTask DisposeAsync()
        {
            _im?.Subscriptions.RemoveAll();
            lock (_lock)
            {
                _window?.Timer?.Cancel();
                _failSafe?.Timer?.Cancel();
            }

            _mdns?.Dispose();
            _transport?.Dispose();
            _started = false;
            await _persistGate.WaitAsync();
            _persistGate.Release();
            foreach (var f in _fabrics)
            {
                f.Dispose();
            }
        }

        private async Task HandleSecureChannelAsync(MessageExchange exchange, MatterMessage message)
        {
            switch (message.Opcode)
            {
                case SecureChannelOpcode.PbkdfParamRequest:
                    await _pase.HandleAsync(exchange, message);
                    break;
                case SecureChannelOpcode.Sigma1:
                    await _case.HandleAsync(exchange, message);
                    break;
                case SecureChannelOpcode.StatusReport:
                    if (exchange.Session is SecureSession secure)
                    {
                        var report = StatusReport.Decode(message.Payload);
                        if (report.ProtocolCode == SecureChannelStatus.CloseSession)
                        {
                            _logger.LogDebug("Peer closed session {Session}", secure.LocalSessionId);
                            _ = Task.Delay(200).ContinueWith(_ => _sessions.Remove(secure));
                        }
                    }

                    break;
            }
        }

        // ---------------------------------------------------------------- persistence

        internal void RequestPersist()
        {
            _ = PersistAsync();
        }

        private async Task PersistAsync()
        {
            string json;
            lock (_lock)
            {
                State.Fabrics = _fabrics.Select(StoredFabric.From).ToList();
                json = State.Serialize();
            }

            await _persistGate.WaitAsync();
            try
            {
                await _storage.SaveAsync(json);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Saving the Matter state failed");
            }
            finally
            {
                _persistGate.Release();
            }
        }

        public Task SaveAsync() => PersistAsync();

        // ---------------------------------------------------------------- commissioning window

        PasscodeVerifier IPaseContext.CurrentVerifier
        {
            get
            {
                lock (_lock)
                {
                    return _window?.Verifier;
                }
            }
        }

        void IPaseContext.OnPaseEstablished(SecureSession session)
        {
        }

        void IPaseContext.OnPaseFailed()
        {
            lock (_lock)
            {
                if (_window != null && ++_window.FailedAttempts >= MaxPaseAttempts)
                {
                    _logger.LogWarning("Too many failed PASE attempts, closing the commissioning window");
                    CloseWindowLocked();
                }
            }

            WindowChanged();
        }

        public byte WindowStatus
        {
            get
            {
                lock (_lock)
                {
                    return (byte)(_window?.Mode == 2 ? 1 : 0);
                }
            }
        }

        public byte? AdminFabricIndex
        {
            get
            {
                lock (_lock)
                {
                    return _window?.AdminFabricIndex;
                }
            }
        }

        public ushort? AdminVendorId
        {
            get
            {
                lock (_lock)
                {
                    return _window?.AdminVendorId;
                }
            }
        }

        /// <summary>Opens a commissioning window with the node's own setup code (for pairing another ecosystem).</summary>
        public void OpenCommissioningWindow(TimeSpan? duration = null)
        {
            lock (_lock)
            {
                CloseWindowLocked();
                _window = new CommissioningWindow { Verifier = _defaultVerifier, Mode = 1, Discriminator = State.Discriminator };
                StartWindowTimerLocked(duration);
            }

            WindowChanged();
        }

        public void CloseCommissioningWindow()
        {
            lock (_lock)
            {
                CloseWindowLocked();
            }

            WindowChanged();
        }

        internal byte? OpenEnhancedCommissioningWindow(SecureSession session, ushort timeout, byte[] verifier, ushort discriminator, uint iterations, byte[] salt)
        {
            if (timeout is < 180 or > 900)
            {
                throw new MatterStatusException(InteractionModel.StatusCode.InvalidCommand);
            }

            lock (_lock)
            {
                if (_failSafe != null || _window?.Mode == 2 || (_window != null && _fabrics.Count == 0))
                {
                    return AdministratorCommissioningCluster.StatusBusy;
                }

                if (iterations is < 1000 or > 100000 || salt.Length is < 16 or > 32 || verifier.Length != 97 || discriminator > 0x0FFF)
                {
                    return AdministratorCommissioningCluster.StatusPakeParameterError;
                }

                CloseWindowLocked();
                _window = new CommissioningWindow
                {
                    Verifier = PasscodeVerifier.FromSerialized(verifier, salt, (int)iterations),
                    Mode = 2,
                    Discriminator = discriminator,
                    AdminFabricIndex = session.FabricIndex,
                    AdminVendorId = session.Fabric?.VendorId
                };
                StartWindowTimerLocked(TimeSpan.FromSeconds(timeout));
            }

            WindowChanged();
            return null;
        }

        internal bool RevokeCommissioning()
        {
            bool wasOpen;
            lock (_lock)
            {
                if (_failSafe != null)
                {
                    ExpireFailSafeLocked();
                }

                wasOpen = _window != null;
                CloseWindowLocked();
            }

            WindowChanged();
            return wasOpen;
        }

        private void StartWindowTimerLocked(TimeSpan? duration)
        {
            if (!duration.HasValue)
            {
                return;
            }

            var window = _window;
            var cts = window.Timer = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(duration.Value, cts.Token);
                    lock (_lock)
                    {
                        if (_window != window)
                        {
                            return;
                        }

                        _logger.LogInformation("Commissioning window timed out");
                        CloseWindowLocked();
                    }

                    WindowChanged();
                }
                catch (OperationCanceledException)
                {
                    // closed earlier
                }
            });
        }

        private void CloseWindowLocked()
        {
            if (_window == null)
            {
                return;
            }

            _window.Timer?.Cancel();
            _window = null;
        }

        private void WindowChanged()
        {
            if (_started)
            {
                CommissioningWindow window;
                lock (_lock)
                {
                    window = _window;
                }

                if (window != null)
                {
                    _mdns?.AdvertiseCommissionable(new CommissionableInfo
                    {
                        Discriminator = window.Discriminator,
                        VendorId = Attestation.VendorId,
                        ProductId = Attestation.ProductId,
                        DeviceName = Options.DeviceName,
                        DeviceType = Options.PrimaryDeviceType,
                        CommissioningMode = window.Mode
                    });
                }
                else
                {
                    _mdns?.StopCommissionable();
                }
            }

            var admin = DataModel.GetEndpoint(0)?.GetCluster(AdministratorCommissioningCluster.Id);
            admin?.MarkChanged(0);
            admin?.MarkChanged(1);
            admin?.MarkChanged(2);
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateOperationalAdvertisements()
        {
            if (!_started)
            {
                return;
            }

            _mdns?.SetOperational(Fabrics.Select(f => (f.OperationalInstanceName, f.CompressedFabricIdHex)));
        }

        private void FabricsChanged()
        {
            UpdateOperationalAdvertisements();
            var credentials = DataModel.GetEndpoint(0)?.GetCluster(OperationalCredentialsCluster.Id);
            foreach (var a in new uint[] { 0, 1, 3, 4 })
            {
                credentials?.MarkChanged(a);
            }

            DataModel.GetEndpoint(0)?.GetCluster(AccessControlCluster.Id)?.MarkChanged(0);
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        // ---------------------------------------------------------------- fail-safe

        internal byte ArmFailSafe(SecureSession session, ushort seconds, ulong breadcrumb)
        {
            lock (_lock)
            {
                if (_failSafe != null && _failSafe.FabricIndex != session.FabricIndex)
                {
                    return CommissioningError.BusyWithOtherAdmin;
                }

                if (seconds == 0)
                {
                    if (_failSafe != null)
                    {
                        ExpireFailSafeLocked();
                    }

                    return CommissioningError.Ok;
                }

                if (_failSafe == null)
                {
                    if (session.IsPase && _window == null)
                    {
                        return CommissioningError.BusyWithOtherAdmin;
                    }

                    _failSafe = new FailSafeContext { FabricIndex = session.FabricIndex };
                }

                _failSafe.Timer?.Cancel();
                var context = _failSafe;
                var cts = context.Timer = new CancellationTokenSource();
                var duration = TimeSpan.FromSeconds(Math.Min(seconds, (ushort)900));
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(duration, cts.Token);
                        lock (_lock)
                        {
                            if (_failSafe != context)
                            {
                                return;
                            }

                            _logger.LogWarning("Fail-safe expired, rolling back the commissioning");
                            ExpireFailSafeLocked();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // re-armed or completed
                    }
                });

                Breadcrumb = breadcrumb;
                return CommissioningError.Ok;
            }
        }

        private void ExpireFailSafeLocked()
        {
            var fs = _failSafe;
            _failSafe = null;
            fs.Timer?.Cancel();
            if (fs.AddedFabric != null)
            {
                var index = fs.AddedFabric.FabricIndex;
                _ = Task.Run(() =>
                {
                    _im?.Subscriptions.RemoveForFabric(index);
                    _sessions?.RemoveForFabric(index);
                });
                fs.AddedFabric.Dispose();
            }

            if (fs.UpdateBackup is { } backup)
            {
                backup.Fabric.OperationalKey.Dispose();
                backup.Fabric.Noc = backup.Noc;
                backup.Fabric.Icac = backup.Icac;
                backup.Fabric.OperationalKey = backup.Key;
                backup.Fabric.Initialize();

                // UpdateNOC already saved the new certificate; save the restored one as well.
                RequestPersist();
            }

            fs.PendingKey?.Dispose();
            Breadcrumb = 0;
            _ = Task.Run(() =>
            {
                _sessions?.RemovePaseSessions();
                FabricsChanged();
            });
        }

        internal byte CompleteCommissioning(SecureSession session)
        {
            Fabric added;
            lock (_lock)
            {
                if (_failSafe == null)
                {
                    return CommissioningError.NoFailSafe;
                }

                if (session.IsPase || session.FabricIndex != _failSafe.FabricIndex)
                {
                    return CommissioningError.InvalidAuthentication;
                }

                var fs = _failSafe;
                _failSafe = null;
                fs.Timer?.Cancel();
                fs.PendingKey?.Dispose();
                added = fs.AddedFabric;
                if (added != null)
                {
                    _fabrics.Add(added);
                    CloseWindowLocked();
                }

                if (fs.UpdateBackup is { } backup)
                {
                    backup.Key.Dispose();
                }

                Breadcrumb = 0;
            }

            RequestPersist();
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                _sessions?.RemovePaseSessions();
            });

            if (added != null)
            {
                _logger.LogInformation("Commissioning complete: fabric {Index} (fabric id {FabricId:X16}, vendor 0x{Vendor:X4})", added.FabricIndex, added.FabricId, added.VendorId);
                WindowChanged();
            }

            FabricsChanged();
            return CommissioningError.Ok;
        }

        private FailSafeContext RequireFailSafe()
        {
            return _failSafe ?? throw new MatterStatusException(InteractionModel.StatusCode.FailsafeRequired);
        }

        // ---------------------------------------------------------------- operational credentials

        internal void AddTrustedRoot(SecureSession session, byte[] certificate)
        {
            lock (_lock)
            {
                var fs = RequireFailSafe();
                if (fs.PendingRoot != null || fs.NocAdded)
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.ConstraintError);
                }

                MatterCertificate root;
                try
                {
                    root = new MatterCertificate(certificate);
                }
                catch (MatterProtocolException)
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.InvalidCommand);
                }

                if (!root.IsCa || !root.VerifySignature(root.PublicKey))
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.InvalidCommand);
                }

                fs.PendingRoot = certificate;
            }
        }

        internal byte[] CreateCsr(SecureSession session, bool forUpdate)
        {
            lock (_lock)
            {
                var fs = RequireFailSafe();
                if (fs.NocAdded)
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.ConstraintError);
                }

                if (forUpdate && (session.IsPase || session.Fabric == null))
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.InvalidCommand);
                }

                fs.PendingKey?.Dispose();
                fs.PendingKey = MatterCrypto.CreateSigningKey();
                fs.KeyForUpdate = forUpdate;
                var request = new CertificateRequest("O=CSR", fs.PendingKey, HashAlgorithmName.SHA256);
                return request.CreateSigningRequest();
            }
        }

        internal (byte Status, byte FabricIndex) AddNoc(SecureSession session, byte[] noc, byte[] icac, byte[] ipk, ulong caseAdminSubject, ushort adminVendorId)
        {
            Fabric fabric;
            lock (_lock)
            {
                var fs = RequireFailSafe();
                if (fs.NocAdded)
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.ConstraintError);
                }

                if (fs.PendingKey == null || fs.KeyForUpdate)
                {
                    return (NocStatus.MissingCsr, 0);
                }

                if (fs.PendingRoot == null || ipk.Length != 16)
                {
                    return (NocStatus.InvalidNoc, 0);
                }

                if (_fabrics.Count >= SupportedFabrics)
                {
                    return (NocStatus.TableFull, 0);
                }

                if (!IsOperationalNodeId(caseAdminSubject) && !IsCatSubject(caseAdminSubject))
                {
                    return (NocStatus.InvalidAdminSubject, 0);
                }

                MatterCertificate nocCert;
                byte[] rootPublicKey;
                try
                {
                    var root = new MatterCertificate(fs.PendingRoot);
                    rootPublicKey = root.PublicKey;
                    nocCert = new MatterCertificate(noc);
                    var issuer = rootPublicKey;
                    if (icac != null)
                    {
                        var icacCert = new MatterCertificate(icac);
                        if (!icacCert.VerifySignature(rootPublicKey))
                        {
                            return (NocStatus.InvalidNoc, 0);
                        }

                        if (icacCert.SubjectFabricId.HasValue && icacCert.SubjectFabricId != nocCert.SubjectFabricId)
                        {
                            return (NocStatus.InvalidNoc, 0);
                        }

                        issuer = icacCert.PublicKey;
                    }

                    if (!nocCert.VerifySignature(issuer))
                    {
                        return (NocStatus.InvalidNoc, 0);
                    }
                }
                catch (MatterProtocolException)
                {
                    return (NocStatus.InvalidNoc, 0);
                }

                if (!nocCert.PublicKey.AsSpan().SequenceEqual(MatterCrypto.ExportPublicKey(fs.PendingKey)))
                {
                    return (NocStatus.InvalidPublicKey, 0);
                }

                if (nocCert.SubjectNodeId is not { } nodeId || !IsOperationalNodeId(nodeId))
                {
                    return (NocStatus.InvalidNodeOpId, 0);
                }

                if (nocCert.SubjectFabricId is not { } fabricId || fabricId == 0)
                {
                    return (NocStatus.InvalidNoc, 0);
                }

                if (_fabrics.Any(f => f.FabricId == fabricId && f.RootPublicKey.AsSpan().SequenceEqual(rootPublicKey)))
                {
                    return (NocStatus.FabricConflict, 0);
                }

                var index = AllocateFabricIndexLocked();
                fabric = new Fabric
                {
                    FabricIndex = index,
                    RootCert = fs.PendingRoot,
                    Noc = noc,
                    Icac = icac,
                    IpkEpochKey = ipk,
                    VendorId = adminVendorId,
                    CaseAdminSubject = caseAdminSubject,
                    OperationalKey = fs.PendingKey,
                    Acl = new List<AclEntry>
                    {
                        new()
                        {
                            Privilege = AclPrivilege.Administer,
                            AuthMode = AclAuthMode.Case,
                            Subjects = new List<ulong> { caseAdminSubject },
                            FabricIndex = index
                        }
                    }
                };
                fabric.Initialize();

                fs.PendingKey = null;
                fs.AddedFabric = fabric;
                fs.NocAdded = true;
                fs.FabricIndex = index;
                if (session.IsPase)
                {
                    session.Fabric = fabric;
                }
            }

            _logger.LogInformation("AddNOC: fabric {Index} for node {Node:X16} (vendor 0x{Vendor:X4})", fabric.FabricIndex, fabric.NodeId, adminVendorId);
            FabricsChanged();
            return (NocStatus.Ok, fabric.FabricIndex);
        }

        internal (byte Status, byte FabricIndex) UpdateNoc(SecureSession session, byte[] noc, byte[] icac)
        {
            Fabric fabric;
            lock (_lock)
            {
                var fs = RequireFailSafe();
                fabric = session.IsPase ? null : session.Fabric;
                if (fabric == null || !_fabrics.Contains(fabric))
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.InvalidCommand);
                }

                if (fs.NocAdded)
                {
                    throw new MatterStatusException(InteractionModel.StatusCode.ConstraintError);
                }

                if (fs.PendingKey == null || !fs.KeyForUpdate)
                {
                    return (NocStatus.MissingCsr, 0);
                }

                MatterCertificate nocCert;
                try
                {
                    nocCert = CaseResponder.ValidateChain(fabric, noc, icac);
                }
                catch (MatterProtocolException)
                {
                    return (NocStatus.InvalidNoc, 0);
                }

                if (!nocCert.PublicKey.AsSpan().SequenceEqual(MatterCrypto.ExportPublicKey(fs.PendingKey)))
                {
                    return (NocStatus.InvalidPublicKey, 0);
                }

                fs.UpdateBackup = (fabric, fabric.Noc, fabric.Icac, fabric.OperationalKey);
                fabric.Noc = noc;
                fabric.Icac = icac;
                fabric.OperationalKey = fs.PendingKey;
                fabric.Initialize();
                fs.PendingKey = null;
                fs.NocAdded = true;
            }

            RequestPersist();
            FabricsChanged();
            return (NocStatus.Ok, fabric.FabricIndex);
        }

        internal (byte Status, byte FabricIndex) UpdateFabricLabel(SecureSession session, string label)
        {
            var fabric = session.Fabric ?? throw new MatterStatusException(InteractionModel.StatusCode.UnsupportedAccess);
            if (label.Length > 32)
            {
                throw new MatterStatusException(InteractionModel.StatusCode.ConstraintError);
            }

            lock (_lock)
            {
                if (label.Length > 0 && Fabrics.Any(f => f != fabric && f.Label == label))
                {
                    return (NocStatus.LabelConflict, 0);
                }

                fabric.Label = label;
            }

            RequestPersist();
            FabricsChanged();
            return (NocStatus.Ok, fabric.FabricIndex);
        }

        internal (byte Status, byte FabricIndex) RemoveFabric(byte fabricIndex)
        {
            Fabric fabric;
            bool none;
            lock (_lock)
            {
                fabric = _fabrics.FirstOrDefault(f => f.FabricIndex == fabricIndex);
                if (fabric == null)
                {
                    return (NocStatus.InvalidFabricIndex, 0);
                }

                _fabrics.Remove(fabric);
                none = _fabrics.Count == 0;
            }

            _logger.LogInformation("Fabric {Index} removed", fabricIndex);
            RequestPersist();
            _ = Task.Run(async () =>
            {
                // let the NOCResponse go out on the session that is about to be closed
                await Task.Delay(1000);
                _im?.Subscriptions.RemoveForFabric(fabricIndex);
                _sessions?.RemoveForFabric(fabricIndex);
                fabric.Dispose();
            });

            FabricsChanged();
            if (none)
            {
                OpenCommissioningWindow();
            }

            return (NocStatus.Ok, fabricIndex);
        }

        /// <summary>Removes all fabrics (the controllers lose the bridge) and opens the commissioning window.</summary>
        public void RemoveAllFabrics()
        {
            List<Fabric> removed;
            lock (_lock)
            {
                if (_failSafe != null)
                {
                    ExpireFailSafeLocked();
                }

                removed = _fabrics.ToList();
                _fabrics.Clear();
            }

            foreach (var f in removed)
            {
                _im?.Subscriptions.RemoveForFabric(f.FabricIndex);
                _sessions?.RemoveForFabric(f.FabricIndex);
                f.Dispose();
            }

            _sessions?.RemovePaseSessions();
            RequestPersist();
            FabricsChanged();
            OpenCommissioningWindow();
        }

        private byte AllocateFabricIndexLocked()
        {
            var used = Fabrics.Select(f => f.FabricIndex).ToHashSet();
            var index = State.NextFabricIndex;
            for (var i = 0; i < 254; i++)
            {
                if (index is 0 or 255)
                {
                    index = 1;
                }

                if (!used.Contains(index))
                {
                    State.NextFabricIndex = (byte)(index == 254 ? 1 : index + 1);
                    return index;
                }

                index++;
            }

            throw new MatterStatusException(InteractionModel.StatusCode.ResourceExhausted);
        }

        private static bool IsOperationalNodeId(ulong id) => id is >= 1 and <= 0xFFFF_FFEF_FFFF_FFFF;

        private static bool IsCatSubject(ulong id) => (id >> 32) == 0xFFFF_FFFD && (ushort)id != 0;
    }
}
