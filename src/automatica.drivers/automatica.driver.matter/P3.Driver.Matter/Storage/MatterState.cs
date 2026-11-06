using System.Security.Cryptography;
using System.Text.Json;
using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Storage
{
    /// <summary>Where the node keeps its persistent state (a JSON document).</summary>
    public interface IMatterStorage
    {
        Task<string> LoadAsync(CancellationToken token = default);
        Task SaveAsync(string json, CancellationToken token = default);
    }

    public sealed class FileMatterStorage : IMatterStorage
    {
        private readonly string _path;

        public FileMatterStorage(string path)
        {
            _path = path;
        }

        public async Task<string> LoadAsync(CancellationToken token = default)
        {
            return File.Exists(_path) ? await File.ReadAllTextAsync(_path, token) : null;
        }

        public async Task SaveAsync(string json, CancellationToken token = default)
        {
            var tmp = _path + ".tmp";
            await File.WriteAllTextAsync(tmp, json, token);
            File.Move(tmp, _path, true);
        }
    }

    public sealed class InMemoryMatterStorage : IMatterStorage
    {
        public string Json { get; set; }

        public Task<string> LoadAsync(CancellationToken token = default) => Task.FromResult(Json);

        public Task SaveAsync(string json, CancellationToken token = default)
        {
            Json = json;
            return Task.CompletedTask;
        }
    }

    public sealed class StoredFabric
    {
        public byte FabricIndex { get; set; }
        public string RootCert { get; set; }
        public string Noc { get; set; }
        public string Icac { get; set; }
        public string IpkEpochKey { get; set; }
        public ushort VendorId { get; set; }
        public ulong CaseAdminSubject { get; set; }
        public string Label { get; set; }
        public string OperationalKey { get; set; }
        public List<string> Acl { get; set; } = new();

        public static StoredFabric From(Fabric f) => new()
        {
            FabricIndex = f.FabricIndex,
            RootCert = Convert.ToBase64String(f.RootCert),
            Noc = Convert.ToBase64String(f.Noc),
            Icac = f.Icac == null ? null : Convert.ToBase64String(f.Icac),
            IpkEpochKey = Convert.ToBase64String(f.IpkEpochKey),
            VendorId = f.VendorId,
            CaseAdminSubject = f.CaseAdminSubject,
            Label = f.Label,
            OperationalKey = Convert.ToBase64String(f.OperationalKey.ExportPkcs8PrivateKey()),
            Acl = f.Acl.Select(a => Convert.ToBase64String(a.Encode())).ToList()
        };

        public Fabric ToFabric()
        {
            var key = ECDsa.Create();
            key.ImportPkcs8PrivateKey(Convert.FromBase64String(OperationalKey), out _);
            var fabric = new Fabric
            {
                FabricIndex = FabricIndex,
                RootCert = Convert.FromBase64String(RootCert),
                Noc = Convert.FromBase64String(Noc),
                Icac = Icac == null ? null : Convert.FromBase64String(Icac),
                IpkEpochKey = Convert.FromBase64String(IpkEpochKey),
                VendorId = VendorId,
                CaseAdminSubject = CaseAdminSubject,
                Label = Label ?? "",
                OperationalKey = key,
                Acl = Acl.Select(a => AclEntry.Read(TlvReader.Decode(Convert.FromBase64String(a)), FabricIndex)).ToList()
            };
            fabric.Initialize();
            return fabric;
        }
    }

    public sealed class MatterState
    {
        public uint Passcode { get; set; }
        public ushort Discriminator { get; set; }
        public string Salt { get; set; }
        public int Iterations { get; set; } = 1000;
        public byte NextFabricIndex { get; set; } = 1;
        public ushort NextEndpointId { get; set; } = 2;
        public ushort RebootCount { get; set; }
        public string UniqueId { get; set; }
        public string NodeLabel { get; set; }
        public string Location { get; set; } = "XX";
        public List<StoredFabric> Fabrics { get; set; } = new();
        public Dictionary<string, ushort> Endpoints { get; set; } = new();

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

        public string Serialize() => JsonSerializer.Serialize(this, Options);

        public static MatterState Deserialize(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<MatterState>(json, Options);
        }

        /// <summary>A fresh identity with a random, spec-valid passcode and discriminator.</summary>
        public static MatterState CreateNew()
        {
            return new MatterState
            {
                Passcode = Commissioning.SetupPayload.GeneratePasscode(),
                Discriminator = (ushort)RandomNumberGenerator.GetInt32(0, 0x1000),
                Salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                Iterations = 1000,
                UniqueId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
            };
        }
    }
}
