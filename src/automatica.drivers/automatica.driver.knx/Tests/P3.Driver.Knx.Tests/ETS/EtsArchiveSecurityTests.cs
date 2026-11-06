using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Ets;
using Xunit;
using SharpZipEntry = ICSharpCode.SharpZipLib.Zip.ZipEntry;

namespace P3.Driver.Knx.Tests.ETS
{
    public class EtsArchiveSecurityTests : IDisposable
    {
        private readonly string _workDir;

        public EtsArchiveSecurityTests()
        {
            _workDir = Path.Combine(Path.GetTempPath(), "ets-security-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_workDir))
            {
                Directory.Delete(_workDir, true);
            }
        }

        private static string DeriveEts6Password(string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(Encoding.Unicode.GetBytes(password), Encoding.ASCII.GetBytes("21.project.ets.knx.org"), 65536, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        /// <summary>
        /// Rebuilds the simple three level fixture as an ETS6 export with an AES encrypted project archive
        /// </summary>
        private string CreateEts6PasswordProtectedProject(string password)
        {
            var innerZip = new MemoryStream();
            using (var source = System.IO.Compression.ZipFile.OpenRead(Path.Combine("ETS", "ETS5_Simple_ThreeLevel.knxproj")))
            {
                using (var inner = new ZipOutputStream(innerZip) { IsStreamOwner = false, Password = DeriveEts6Password(password) })
                {
                    foreach (var entry in source.Entries)
                    {
                        if (!entry.FullName.StartsWith("P-0839/") || entry.FullName.Contains("UserFiles"))
                        {
                            continue;
                        }

                        inner.PutNextEntry(new SharpZipEntry(entry.Name) { AESKeySize = 256 });
                        using (var data = entry.Open())
                        {
                            data.CopyTo(inner);
                        }
                        inner.CloseEntry();
                    }
                }

                var target = Path.Combine(_workDir, "ets6.knxproj");
                using (var outer = System.IO.Compression.ZipFile.Open(target, ZipArchiveMode.Create))
                {
                    foreach (var entry in source.Entries)
                    {
                        if (entry.FullName.StartsWith("P-0839"))
                        {
                            continue;
                        }

                        using var from = entry.Open();
                        using var to = outer.CreateEntry(entry.FullName).Open();
                        from.CopyTo(to);
                    }

                    using var project = outer.CreateEntry("P-0839.zip").Open();
                    innerZip.Position = 0;
                    innerZip.CopyTo(project);
                }

                return target;
            }
        }

        private string CreateZip(string name, params (string Entry, string Content)[] entries)
        {
            var target = Path.Combine(_workDir, name);
            using (var zip = System.IO.Compression.ZipFile.Open(target, ZipArchiveMode.Create))
            {
                foreach (var (entry, content) in entries)
                {
                    using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
                    writer.Write(content);
                }
            }
            return target;
        }

        [Fact]
        public void Ets6PasswordProtectedProjectIsImported()
        {
            var file = CreateEts6PasswordProtectedProject("secret");

            var project = new EtsProjectParser().ParseEtsFile(file, "secret");

            Assert.Equal(GroupAddressStyle.ThreeLevel, project.GroupAddressStyle);
            Assert.NotEmpty(project.Children);
        }

        [Fact]
        public void Ets6PasswordProtectedProjectNeedsThePassword()
        {
            var file = CreateEts6PasswordProtectedProject("secret");

            Assert.Throws<EtsProjectParserPasswordRequiredException>(() => new EtsProjectParser().ParseEtsFile(file));
            Assert.ThrowsAny<EtsProjectParserException>(() => new EtsProjectParser().ParseEtsFile(file, "wrong"));
        }

        [Theory]
        [InlineData("../evil.xml")]
        [InlineData("P-0001/../../evil.xml")]
        [InlineData("..\\evil.xml")]
        [InlineData("C:/evil.xml")]
        public void EntriesOutsideTheTargetDirectoryAreRejected(string entryName)
        {
            var file = CreateZip("slip.knxproj", ("P-0001/0.xml", "<KNX/>"), (entryName, "<KNX/>"));

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(file));
            Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), "evil.xml")));
        }

        [Fact]
        public void ArchivesAboveTheSizeLimitAreRejected()
        {
            var file = CreateZip("big.zip", ("a.xml", new string('a', 4096)), ("b.xml", new string('b', 4096)));
            var extractor = new EtsZipExtractor { MaxTotalSize = 6000 };

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => extractor.Extract(file, Path.Combine(_workDir, "big")));
        }

        [Fact]
        public void ArchivesWithTooManyEntriesAreRejected()
        {
            var file = CreateZip("many.zip", ("a.xml", "a"), ("b.xml", "b"), ("c.xml", "c"));
            var extractor = new EtsZipExtractor { MaxEntries = 2 };

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => extractor.Extract(file, Path.Combine(_workDir, "many")));
        }

        [Fact]
        public void DtdsAreNotProcessed()
        {
            var xml = "<?xml version=\"1.0\"?><!DOCTYPE KNX [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\">]><KNX xmlns=\"http://knx.org/xml/project/14\"><Project Id=\"P-0001\">&b;</Project></KNX>";
            var file = CreateZip("dtd.knxproj", ("P-0001/0.xml", xml));

            Assert.ThrowsAny<EtsProjectParserException>(() => new EtsProjectParser().ParseEtsFile(file));
        }

        [Theory]
        [InlineData("project.knxproj", true)]
        [InlineData("../project.knxproj", false)]
        [InlineData("sub/project.knxproj", false)]
        [InlineData("", false)]
        public void ImportFileMustBeInTheTempDirectory(string fileName, bool allowed)
        {
            var resolved = Knx3Level.ResolveImportFile(_workDir, fileName);

            Assert.Equal(allowed, resolved != null);
            if (allowed)
            {
                Assert.Equal(Path.Combine(_workDir, fileName), resolved);
            }
        }

        [Fact]
        public void ImportFileMayBeTheFullUploadPath()
        {
            var full = Path.Combine(_workDir, "upload.knxproj");

            Assert.Equal(full, Knx3Level.ResolveImportFile(_workDir, full));
            Assert.Null(Knx3Level.ResolveImportFile(_workDir, Path.Combine(Path.GetTempPath(), "other", "upload.knxproj")));
        }
    }
}
