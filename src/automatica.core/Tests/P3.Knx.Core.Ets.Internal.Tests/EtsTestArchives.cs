using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ZipOutputStream = ICSharpCode.SharpZipLib.Zip.ZipOutputStream;
using SharpZipEntry = ICSharpCode.SharpZipLib.Zip.ZipEntry;

namespace P3.Knx.Core.Ets.Internal.Tests
{
    /// <summary>
    /// Builds ETS project archives (.knxproj) for the tests in a private temp directory.
    /// </summary>
    internal sealed class EtsTestArchives : IDisposable
    {
        public const string Ns = "http://knx.org/xml/project/20";

        public EtsTestArchives()
        {
            WorkDir = Path.Combine(Path.GetTempPath(), "ets-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(WorkDir);
        }

        public string WorkDir { get; }

        public void Dispose()
        {
            if (Directory.Exists(WorkDir))
            {
                Directory.Delete(WorkDir, true);
            }
        }

        public static (string Name, byte[] Data) Text(string name, string content) => (name, Encoding.UTF8.GetBytes(content));

        /// <summary>
        /// Plain archive, the entry names are stored as given (also "../" and drive letters).
        /// </summary>
        public string Zip(string fileName, params (string Name, byte[] Data)[] entries)
        {
            var target = Path.Combine(WorkDir, fileName);
            using (var zip = System.IO.Compression.ZipFile.Open(target, ZipArchiveMode.Create))
            {
                foreach (var (name, data) in entries)
                {
                    using var stream = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
                    stream.Write(data, 0, data.Length);
                }
            }
            return target;
        }

        /// <summary>
        /// Password protected archive, AES 256 (ETS6) or ZipCrypto.
        /// </summary>
        public static byte[] EncryptedZip(string password, bool aes, params (string Name, byte[] Data)[] entries)
        {
            var buffer = new MemoryStream();
            using (var zip = new ZipOutputStream(buffer) { IsStreamOwner = false, Password = password })
            {
                foreach (var (name, data) in entries)
                {
                    zip.PutNextEntry(new SharpZipEntry(name) { AESKeySize = aes ? 256 : 0 });
                    zip.Write(data, 0, data.Length);
                    zip.CloseEntry();
                }
            }
            return buffer.ToArray();
        }

        /// <summary>
        /// The key ETS6 derives from the project password for the inner project archive.
        /// </summary>
        public static string DeriveEts6Password(string password)
        {
            var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(password), Encoding.ASCII.GetBytes("21.project.ets.knx.org"), 65536,
                HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(key);
        }

        /// <summary>
        /// Overwrites the uncompressed size of the first entry in the local and central headers (a forged zip bomb).
        /// </summary>
        public static void ForgeUncompressedSize(string zipFile, uint size)
        {
            var data = File.ReadAllBytes(zipFile);
            var patched = 0;
            for (var i = 0; i < data.Length - 4; i++)
            {
                if (data[i] != 0x50 || data[i + 1] != 0x4B)
                {
                    continue;
                }

                if (data[i + 2] == 0x03 && data[i + 3] == 0x04)
                {
                    BitConverter.GetBytes(size).CopyTo(data, i + 22);
                    patched++;
                }
                else if (data[i + 2] == 0x01 && data[i + 3] == 0x02)
                {
                    BitConverter.GetBytes(size).CopyTo(data, i + 24);
                    patched++;
                }
            }

            if (patched < 2)
            {
                throw new InvalidOperationException("zip headers not found");
            }
            File.WriteAllBytes(zipFile, data);
        }

        // ---- ETS project xml ----

        public static string ProjectXml(string projectId, string name, string style) =>
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?><KNX xmlns=\"{Ns}\" CreatedBy=\"ETS5\"><Project Id=\"{projectId}\"><ProjectInformation Name=\"{name}\" GroupAddressStyle=\"{style}\" /></Project></KNX>";

        public static string InstallationXml(string projectId, string groupRanges, string devices = "") =>
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?><KNX xmlns=\"{Ns}\"><Project Id=\"{projectId}\"><Installations><Installation Name=\"\" InstallationId=\"0\">" +
            $"<Topology><Area Id=\"{projectId}-0_A-1\" Address=\"1\"><Line Id=\"{projectId}-0_L-1\" Address=\"1\">{devices}</Line></Area></Topology>" +
            $"<GroupAddresses><GroupRanges>{groupRanges}</GroupRanges></GroupAddresses></Installation></Installations></Project></KNX>";

        public static string Range(string id, int start, int end, string name, string inner, string description = null) =>
            $"<GroupRange Id=\"{id}\" RangeStart=\"{start}\" RangeEnd=\"{end}\" Name=\"{name}\"{(description != null ? $" Description=\"{description}\"" : "")}>{inner}</GroupRange>";

        public static string Ga(string id, int address, string name, string dpt = null) =>
            $"<GroupAddress Id=\"{id}\" Address=\"{address}\" Name=\"{name}\"{(dpt != null ? $" DatapointType=\"{dpt}\"" : "")} />";

        public static string Device(string id, params (string RefId, string Dpt, string GroupAddressRefId, bool Send)[] comObjects)
        {
            var sb = new StringBuilder($"<DeviceInstance Id=\"{id}\" Address=\"1\"><ComObjectInstanceRefs>");
            foreach (var (refId, dpt, ga, send) in comObjects)
            {
                sb.Append($"<ComObjectInstanceRef RefId=\"{refId}\"{(dpt != null ? $" DatapointType=\"{dpt}\"" : "")}><Connectors>");
                sb.Append(send ? $"<Send GroupAddressRefId=\"{ga}\" />" : $"<Receive GroupAddressRefId=\"{ga}\" />");
                sb.Append("</Connectors></ComObjectInstanceRef>");
            }
            sb.Append("</ComObjectInstanceRefs></DeviceInstance>");
            return sb.ToString();
        }

        /// <summary>
        /// Main group 1 (1/0 with 1/0/1 + 1/0/2, 1/1 with 1/1/10) and main group 2 (2/7 with 2/7/255), 1/0/1 is connected to a device.
        /// </summary>
        public static string MinimalThreeLevelInstallation(string projectId = "P-0001")
        {
            var p = projectId + "-0";
            var ranges =
                Range($"{p}_GR-1", 2048, 4095, "Lighting",
                    Range($"{p}_GR-2", 2048, 2303, "Ground floor",
                        Ga($"{p}_GA-1", 2049, "Kitchen switch", "DPST-1-1") +
                        Ga($"{p}_GA-2", 2050, "Kitchen dim value", "DPST-5-1"), "first floor of the house") +
                    Range($"{p}_GR-3", 2304, 2559, "First floor",
                        Ga($"{p}_GA-3", 2314, "Bath temperature", "DPST-9-1")), "all lights") +
                Range($"{p}_GR-4", 4096, 6143, "Blinds",
                    Range($"{p}_GR-5", 5888, 6143, "Roof",
                        Ga($"{p}_GA-4", 6143, "Roof window", "DPST-1-8")));

            var devices = Device($"{p}_DI-1", ("M-0083_A-0001-01-ABCD_O-0_R-1", "DPST-1-1", $"{p}_GA-1", true));
            return InstallationXml(projectId, ranges, devices);
        }

        public static IEnumerable<(string Name, byte[] Data)> MinimalProjectEntries(string projectId = "P-0001", string style = "ThreeLevel")
        {
            yield return Text("knx_master.xml", $"<KNX xmlns=\"{Ns}\" />");
            yield return Text($"{projectId}/project.xml", ProjectXml(projectId, "Mini", style));
            yield return Text($"{projectId}/0.xml", MinimalThreeLevelInstallation(projectId));
        }
    }
}
