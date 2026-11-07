using System;
using System.IO;
using System.Linq;
using P3.Knx.Core.Ets;
using Xunit;
using static P3.Knx.Core.Ets.Internal.Tests.EtsTestArchives;

namespace P3.Knx.Core.Ets.Internal.Tests
{
    /// <summary>
    /// Safe extraction of ETS archives: zip slip, absolute paths, size and entry limits, password protected project archives.
    /// </summary>
    public sealed class EtsArchiveExtractionTests : IDisposable
    {
        private const string Password = "pr0ject-Passw0rd";

        private readonly EtsTestArchives _archives = new();

        public void Dispose() => _archives.Dispose();

        private string Target(string name) => Path.Combine(_archives.WorkDir, "out-" + name);

        /// <summary>
        /// ETS export with the project directory moved into the encrypted inner archive P-0001.zip.
        /// </summary>
        private string ProtectedProject(string innerPassword, bool aes, params (string Name, byte[] Data)[] innerEntries)
        {
            if (innerEntries.Length == 0)
            {
                innerEntries = new[]
                {
                    Text("project.xml", ProjectXml("P-0001", "Protected", "ThreeLevel")),
                    Text("0.xml", MinimalThreeLevelInstallation()),
                };
            }

            var inner = EncryptedZip(innerPassword, aes, innerEntries);
            return _archives.Zip("protected.knxproj",
                Text("knx_master.xml", $"<KNX xmlns=\"{Ns}\" />"),
                Text("P-0001.signature", "signature"),
                ("P-0001.zip", inner));
        }

        private static string[] GroupAddresses(EtsGroup group) =>
            group.Datapoints.Select(a => a.GetGroupAddress()).Concat(group.SubGroups.SelectMany(GroupAddresses)).ToArray();

        // ---- zip slip / absolute paths ----

        [Theory]
        [InlineData("../{0}")]
        [InlineData("../../{0}")]
        [InlineData("P-0001/../../{0}")]
        [InlineData("..\\{0}")]
        [InlineData("P-0001\\..\\..\\{0}")]
        [InlineData("C:/{0}")]
        [InlineData("C:\\{0}")]
        [InlineData("C:{0}")]
        public void ParseEtsFile_EntryOutsideTheTempDirectory_ThrowsInvalidZipFile(string pattern)
        {
            var evil = $"evil-{Guid.NewGuid():N}.xml";
            var file = _archives.Zip("slip.knxproj", MinimalProjectEntries().Append(Text(string.Format(pattern, evil), "<KNX/>")).ToArray());

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(file));

            Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), evil)));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar))!, evil)));
        }

        [Fact]
        public void ParseEtsFile_LeadingSlashEntries_AreExtractedBelowTheTempDirectory()
        {
            // "/P-0001/0.xml" is not an absolute path inside the archive, it is relative to the extraction root
            var entries = MinimalProjectEntries().Select(a => ("/" + a.Name, a.Data)).ToArray();
            var file = _archives.Zip("slash.knxproj", entries);

            var project = new EtsProjectParser().ParseEtsFile(file);

            Assert.Equal(new[] { "1/0/1", "1/0/2", "1/1/10", "2/7/255" }, GroupAddresses(project));
        }

        [Fact]
        public void ParseEtsFile_BackslashSeparatedEntries_AreParsed()
        {
            var entries = MinimalProjectEntries().Select(a => (a.Name.Replace('/', '\\'), a.Data)).ToArray();
            var file = _archives.Zip("backslash.knxproj", entries);

            Assert.Equal(4, GroupAddresses(new EtsProjectParser().ParseEtsFile(file)).Length);
        }

        [Fact]
        public void ParseEtsFile_ZipSlipInsideTheProtectedProjectArchive_ThrowsInvalidZipFile()
        {
            var evil = $"evil-{Guid.NewGuid():N}.xml";
            var file = ProtectedProject(DeriveEts6Password(Password), true,
                Text("0.xml", MinimalThreeLevelInstallation()),
                Text($"../../../{evil}", "<KNX/>"));

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(file, Password));
            Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), evil)));
        }

        [Theory]
        [InlineData("a.xml")]
        [InlineData("dir/a.xml")]
        [InlineData("dir\\a.xml")]
        [InlineData("/a.xml")]
        [InlineData("//a.xml")]
        [InlineData("\\\\server\\share\\a.xml")]
        [InlineData("dir/../a.xml")]
        [InlineData("./a.xml")]
        public void GetSafeDestination_EntryInsideTheRoot_IsBelowTheRoot(string entryName)
        {
            var root = Path.GetFullPath(Target("root")) + Path.DirectorySeparatorChar;

            var destination = EtsZipExtractor.GetSafeDestination(root, entryName);

            Assert.StartsWith(root, destination, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("../a.xml")]
        [InlineData("dir/../../a.xml")]
        [InlineData("..\\a.xml")]
        [InlineData("C:/a.xml")]
        [InlineData("C:\\a.xml")]
        [InlineData("a.xml:stream")]
        [InlineData("a\0.xml")]
        [InlineData("")]
        [InlineData(null)]
        public void GetSafeDestination_EntryOutsideTheRootOrInvalid_Throws(string entryName)
        {
            var root = Path.GetFullPath(Target("root")) + Path.DirectorySeparatorChar;

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => EtsZipExtractor.GetSafeDestination(root, entryName));
        }

        [Fact]
        public void GetSafeDestination_SiblingDirectoryWithTheSamePrefix_IsRejected()
        {
            // root "...\out-root\" must not accept "...\out-root-evil\a.xml"
            var root = Path.GetFullPath(Target("root")) + Path.DirectorySeparatorChar;

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => EtsZipExtractor.GetSafeDestination(root, "../out-root-evil/a.xml"));
        }

        [Fact]
        public void Extract_DirectoryAndFileEntries_AreCreated()
        {
            var file = _archives.Zip("dirs.zip", ("empty/", Array.Empty<byte>()), Text("a/b/c.xml", "<c/>"));
            var target = Target("dirs");

            new EtsZipExtractor().Extract(file, target);

            Assert.True(Directory.Exists(Path.Combine(target, "empty")));
            Assert.Equal("<c/>", File.ReadAllText(Path.Combine(target, "a", "b", "c.xml")));
        }

        [Fact]
        public void Extract_TargetWithoutTrailingSeparator_StillRejectsTheSibling()
        {
            var file = _archives.Zip("sibling.zip", Text("../out-sib-evil/a.xml", "x"));

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsZipExtractor().Extract(file, Target("sib")));
            Assert.False(Directory.Exists(Target("sib-evil")));
        }

        // ---- limits ----

        [Fact]
        public void Limits_Defaults_AreSet()
        {
            var extractor = new EtsZipExtractor();

            Assert.Equal(20000, extractor.MaxEntries);
            Assert.Equal(512L * 1024 * 1024, extractor.MaxEntrySize);
            Assert.Equal(1024L * 1024 * 1024, extractor.MaxTotalSize);
        }

        [Fact]
        public void Extract_MoreEntriesThanAllowed_Throws()
        {
            var file = _archives.Zip("many.zip", Enumerable.Range(0, 11).Select(i => Text($"{i}.xml", "x")).ToArray());

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsZipExtractor { MaxEntries = 10 }.Extract(file, Target("many")));
        }

        [Fact]
        public void Extract_ExactlyTheAllowedEntries_Succeeds()
        {
            var file = _archives.Zip("ten.zip", Enumerable.Range(0, 10).Select(i => Text($"{i}.xml", "x")).ToArray());
            var target = Target("ten");

            new EtsZipExtractor { MaxEntries = 10 }.Extract(file, target);

            Assert.Equal(10, Directory.GetFiles(target).Length);
        }

        [Fact]
        public void Extract_EntryAboveTheEntrySizeLimit_Throws()
        {
            var file = _archives.Zip("large.zip", ("large.xml", new byte[5000]));

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsZipExtractor { MaxEntrySize = 4096 }.Extract(file, Target("large")));
        }

        [Fact]
        public void Extract_ZipBombWithForgedSize_StopsAtTheLimit()
        {
            // 4 MB of zeros compress to a few KB, the headers claim 10 bytes
            var file = _archives.Zip("bomb.zip", ("bomb.xml", new byte[4 * 1024 * 1024]));
            ForgeUncompressedSize(file, 10);
            var target = Target("bomb");
            var extractor = new EtsZipExtractor { MaxEntrySize = 64 * 1024, MaxTotalSize = 128 * 1024 };

            var ex = Record.Exception(() => extractor.Extract(file, target));

            // either the written bytes are counted or SharpZipLib detects the size mismatch: never more than the limit lands on disk
            Assert.True(ex is EtsProjectParserInvalidZipFileException || ex is ICSharpCode.SharpZipLib.Zip.ZipException, ex?.ToString() ?? "no exception");
            var written = Path.Combine(target, "bomb.xml");
            Assert.True(!File.Exists(written) || new FileInfo(written).Length <= 64 * 1024);
        }

        [Fact]
        public void Extract_HighlyCompressedEntryAboveTheTotalLimit_Throws()
        {
            var file = _archives.Zip("total.zip", ("a.xml", new byte[3 * 1024 * 1024]));
            var target = Target("total");

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsZipExtractor { MaxTotalSize = 1024 * 1024 }.Extract(file, target));
            Assert.True(new FileInfo(Path.Combine(target, "a.xml")).Length <= 1024 * 1024);
        }

        [Fact]
        public void Extract_LimitsAreSharedByAllArchivesOfOneInstance()
        {
            // the outer archive and the inner project archive count together
            var first = _archives.Zip("first.zip", ("a.xml", new byte[3000]));
            var second = _archives.Zip("second.zip", ("b.xml", new byte[3000]));
            var extractor = new EtsZipExtractor { MaxTotalSize = 5000 };

            extractor.Extract(first, Target("first"));

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => extractor.Extract(second, Target("second")));
        }

        [Fact]
        public void Extract_EntryCountIsSharedByAllArchivesOfOneInstance()
        {
            var first = _archives.Zip("c1.zip", Text("a.xml", "a"), Text("b.xml", "b"));
            var second = _archives.Zip("c2.zip", Text("c.xml", "c"));
            var extractor = new EtsZipExtractor { MaxEntries = 2 };

            extractor.Extract(first, Target("c1"));

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => extractor.Extract(second, Target("c2")));
        }

        [Fact]
        public void Extract_NotAZip_ThrowsZipException()
        {
            var file = Path.Combine(_archives.WorkDir, "plain.zip");
            File.WriteAllText(file, "plain text, no zip at all ..........................................");

            Assert.ThrowsAny<ICSharpCode.SharpZipLib.Zip.ZipException>(() => new EtsZipExtractor().Extract(file, Target("plain")));
        }

        // ---- password protected projects ----

        [Fact]
        public void ParseEtsFile_Ets6AesProtectedProject_IsParsedWithThePassword()
        {
            var file = ProtectedProject(DeriveEts6Password(Password), true);

            var project = new EtsProjectParser().ParseEtsFile(file, Password);

            Assert.Equal("Protected", project.Name);
            Assert.Equal(new[] { "1/0/1", "1/0/2", "1/1/10", "2/7/255" }, GroupAddresses(project));
        }

        [Fact]
        public void ParseEtsFile_ZipCryptoProtectedProjectWithTheDerivedKey_IsParsed()
        {
            var file = ProtectedProject(DeriveEts6Password(Password), false);

            var project = new EtsProjectParser().ParseEtsFile(file, Password);

            Assert.Equal(4, GroupAddresses(project).Length);
        }

        [Fact(Skip = "BUG: EtsProjectParser always derives the ETS6 key (bool ets6 = true), ETS5 projects protected with the plain password cannot be opened")]
        public void ParseEtsFile_Ets5ZipCryptoProtectedProject_IsParsedWithThePlainPassword()
        {
            var file = ProtectedProject(Password, false);

            var project = new EtsProjectParser().ParseEtsFile(file, Password);

            Assert.Equal(4, GroupAddresses(project).Length);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseEtsFile_ProtectedProjectWithoutPassword_ThrowsPasswordRequired(string password)
        {
            var file = ProtectedProject(DeriveEts6Password(Password), true);

            var ex = Assert.Throws<EtsProjectParserPasswordRequiredException>(() => new EtsProjectParser().ParseEtsFile(file, password));
            Assert.Equal("KNX.ETS.PASSWORD_REQUIRED", ex.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ParseEtsFile_ProtectedProjectWithTheWrongPassword_ThrowsParserException(bool aes)
        {
            var file = ProtectedProject(DeriveEts6Password(Password), aes);

            Assert.ThrowsAny<EtsProjectParserException>(() => new EtsProjectParser().ParseEtsFile(file, "wrong"));
        }

        [Fact]
        public void ParseEtsFile_InnerProjectArchiveIsNotAZip_ThrowsInvalidZipFile()
        {
            var file = _archives.Zip("badinner.knxproj", Text("knx_master.xml", "<KNX/>"), Text("P-0001.zip", "not a zip ...................................................."));

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(file, Password));
        }
    }
}
