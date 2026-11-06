using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Satellite.Abstraction;
using Automatica.Core.Satellite.Abstraction.Model;
using Automatica.Core.Satellite.Runtime;
using Xunit;

namespace Automatica.Core.Satellite.Tests
{
    public class ContainerPolicyTests
    {
        private static readonly string Digest = "sha256:" + new string('a', 64);

        private static ContainerPolicy Policy(string images = null, string devices = "none", string caps = null, IEnumerable<string> existing = null)
        {
            var exists = new HashSet<string>(existing ?? Array.Empty<string>());
            return new ContainerPolicy(images, null, caps, devices, false,
                pattern => exists.Where(e => ContainerPolicy.GlobMatch(pattern, e)).ToList());
        }

        private static ActionRequest Start(string image, string tag = "latest-develop", string source = null, string digest = null) => new()
        {
            Action = SlaveAction.Start,
            ImageName = image,
            Tag = tag,
            ImageSource = source,
            ImageDigest = digest,
            Id = Guid.NewGuid()
        };

        [Theory]
        [InlineData("automaticacore/automatica-plugin-knx", "automaticacore/automatica-plugin-knx")]
        [InlineData("docker.io/automaticacore/automatica-plugin-knx", "automaticacore/automatica-plugin-knx")]
        [InlineData("index.docker.io/automaticacore/x", "automaticacore/x")]
        public void AllowedImages_AreAccepted(string image, string repository)
        {
            Assert.True(Policy().TryGetImage(Start(image), null, out var repo, out var tag, out var reference, out var error), error);
            Assert.Equal(repository, repo);
            Assert.Equal("latest-develop", tag);
            Assert.Equal($"{repository}:latest-develop", reference);
        }

        [Theory]
        [InlineData("evil/img")]
        [InlineData("automaticacore")]
        [InlineData("automaticacorex/img")]
        [InlineData("automaticacore/sub/img")]
        [InlineData("evil.com/automaticacore/img")]
        [InlineData("localhost:5000/automaticacore/img")]
        [InlineData("automaticacore/../evil")]
        [InlineData("automaticacore/img@sha256:aaaa")]
        [InlineData("AutomaticaCore/img")]
        [InlineData("")]
        [InlineData(null)]
        public void OtherImages_AreRejected(string image)
        {
            Assert.False(Policy().TryGetImage(Start(image), null, out _, out _, out _, out _));
        }

        [Fact]
        public void ConfiguredRegistry_CanBeAllowed()
        {
            var policy = Policy("automaticacore/*,registry.example.com/team/*,myorg/exact");

            Assert.True(policy.TryGetImage(Start("registry.example.com/team/img"), null, out _, out _, out var reference, out _));
            Assert.Equal("registry.example.com/team/img:latest-develop", reference);
            Assert.True(policy.TryGetImage(Start("myorg/exact"), null, out _, out _, out _, out _));
            Assert.False(policy.TryGetImage(Start("myorg/other"), null, out _, out _, out _, out _));
        }

        [Fact]
        public void ImageSource_IsRejected_ExceptTheLegacyDefault()
        {
            Assert.False(Policy().TryGetImage(Start("automaticacore/x", source: "http://evil/rootfs.tar"), null, out _, out _, out _, out _));
            Assert.True(Policy().TryGetImage(Start("automaticacore/x", source: "https://hub.docker.com"), null, out _, out _, out _, out _));
        }

        [Theory]
        [InlineData("latest;rm -rf")]
        [InlineData("-bad")]
        [InlineData("a/b")]
        public void InvalidTags_AreRejected(string tag)
        {
            Assert.False(Policy().TryGetImage(Start("automaticacore/x", tag), null, out _, out _, out _, out _));
        }

        [Fact]
        public void TagOverride_IsUsed()
        {
            Assert.True(Policy().TryGetImage(Start("automaticacore/x", "1.0"), "latest", out _, out var tag, out _, out _));
            Assert.Equal("latest", tag);
        }

        [Fact]
        public void Digest_PinsTheImage()
        {
            Assert.True(Policy().TryGetImage(Start("automaticacore/x", digest: Digest), null, out _, out _, out var reference, out _));
            Assert.Equal($"automaticacore/x@{Digest}", reference);

            Assert.False(Policy().TryGetImage(Start("automaticacore/x", digest: "sha256:xyz"), null, out _, out _, out _, out _));
        }

        [Fact]
        public void Devices_OnlyFromTheAllowList()
        {
            var policy = Policy(devices: "none", existing: new[] { "/dev/ttyUSB0", "/dev/mem", "/dev/sda" });
            var request = Start("automaticacore/x");
            request.Devices = new[] { "/dev/ttyUSB0", "/dev/mem", "/dev/sda", "/dev/../dev/mem", "/etc/shadow" };
            var rejected = new List<string>();

            var devices = policy.GetDevices(request, rejected.Add);

            Assert.Equal(new[] { "/dev/ttyUSB0" }, devices);
            Assert.Equal(4, rejected.Count);
        }

        [Fact]
        public void Devices_Auto_PassesExistingSerialDevices()
        {
            var policy = Policy(devices: "auto", existing: new[] { "/dev/ttyUSB0", "/dev/ttyACM1", "/dev/ttyAMA0", "/dev/bus/usb", "/dev/mem", "/dev/kmsg" });

            var devices = policy.GetDevices(Start("automaticacore/x"));

            Assert.Contains("/dev/ttyUSB0", devices);
            Assert.Contains("/dev/ttyACM1", devices);
            Assert.Contains("/dev/ttyAMA0", devices);
            Assert.Contains("/dev/bus/usb", devices);
            Assert.DoesNotContain("/dev/mem", devices);
            Assert.DoesNotContain("/dev/kmsg", devices);
        }

        [Fact]
        public void Capabilities_OnlyFromTheAllowList()
        {
            var request = Start("automaticacore/x");
            request.CapAdd = new[] { "NET_ADMIN", "cap_sys_admin", "SYS_RAWIO" };

            Assert.Empty(Policy().GetCapabilities(request));
            Assert.Equal(new[] { "NET_ADMIN" }, Policy(caps: "CAP_NET_ADMIN").GetCapabilities(request));
        }

        [Fact]
        public void Fingerprint_IsNormalized()
        {
            var hex = new string('A', 64);
            Assert.Equal(new string('a', 64), SatelliteRuntime.NormalizeFingerprint(hex));
            Assert.Equal(new string('a', 64), SatelliteRuntime.NormalizeFingerprint(String.Join(":", Enumerable.Repeat("AA", 32))));
            Assert.Null(SatelliteRuntime.NormalizeFingerprint("abc"));
            Assert.Null(SatelliteRuntime.NormalizeFingerprint(null));
        }

        [Theory]
        [InlineData("watchdog/sat1/11111111-1111-1111-1111-111111111111", "watchdog/sat1/+", true)]
        [InlineData("watchdog/sat1", "watchdog/sat1/+", false)]
        [InlineData("watchdog/sat2/x", "watchdog/sat1/+", false)]
        [InlineData("slave/sat1/reinit", "slave/sat1/reinit", true)]
        public void TopicMatch_UsesTheReceivedTopicAgainstTheFilter(string topic, string filter, bool expected)
        {
            Assert.Equal(expected, ApplicationMessageReceivedHandler.IsMatch(topic, filter));
        }

        [Fact]
        public void Password_HashAndLegacyMigration()
        {
            var hash = UserAuthData.HashPassword("correct horse battery");
            Assert.StartsWith(UserAuthData.HashPrefix, hash);
            Assert.True(UserAuthData.VerifyPassword("correct horse battery", hash, null, out var rehash));
            Assert.False(rehash);
            Assert.False(UserAuthData.VerifyPassword("wrong", hash, null, out _));

            // legacy value from the old appsettings.Development.json: PBKDF2-SHA1, 10k iterations, separate salt
            var legacySalt = Convert.ToBase64String(new byte[16]);
            var legacyHash = Convert.ToBase64String(Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
                "old-password", new byte[16], Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA1, 10000, 32));
            Assert.True(UserAuthData.VerifyPassword("old-password", legacyHash, legacySalt, out var legacyRehash));
            Assert.True(legacyRehash);
            Assert.False(UserAuthData.VerifyPassword("other", legacyHash, legacySalt, out _));
        }
    }
}
