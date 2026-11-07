using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.NetworkPresence.Driver;
using P3.Driver.NetworkPresence.Driver.Network;
using Xunit;

namespace P3.Driver.NetworkPresence.Tests
{
    public class NetworkPresenceDriverTests : DriverFactoryTestBase<NetworkPresenceDriverFactory>
    {
        private const string PhoneA = "DA:A1:19:00:AB:CD";
        private const string PhoneB = "3C:22:FB:01:02:03";
        private static readonly DateTimeOffset T0 = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

        private sealed class FakeNeighbors : INeighborSource
        {
            public List<NeighborEntry> Entries { get; } = new();

            public Task<IReadOnlyList<NeighborEntry>> ReadAsync(CancellationToken token = default) => Task.FromResult<IReadOnlyList<NeighborEntry>>(Entries.ToList());
        }

        private sealed class RecordingProber : IProber
        {
            public List<IReadOnlyCollection<IPAddress>> Calls { get; } = new();

            public Task ProbeAsync(IReadOnlyCollection<IPAddress> addresses, CancellationToken token = default)
            {
                Calls.Add(addresses.ToList());
                return Task.CompletedTask;
            }
        }

        private sealed class Setup
        {
            public NetworkPresenceDriver Driver { get; init; }
            public FakeNeighbors Neighbors { get; init; }
            public RecordingProber Prober { get; init; }
            public NodeInstance Root { get; init; }
        }

        private sealed class PersonNodes
        {
            public NodeInstance Person { get; init; }
            public NodeInstance Home { get; init; }
            public NodeInstance LastSeen { get; init; }
            public NodeInstance DevicesHome { get; init; }
        }

        private sealed class DeviceNodes
        {
            public NodeInstance Connected { get; init; }
            public NodeInstance Ip { get; init; }
        }

        private NodeInstance ValueChild(NodeInstance parent, Guid template)
        {
            var node = CreateNodeInstance(template);
            parent.InverseThis2ParentNodeInstanceNavigation.Add(node);
            return node;
        }

        private NodeInstance CreateRoot(string subnets = "192.168.1.0/24", int awayMinutes = 10)
        {
            var root = CreateNodeInstance(DriverFactory.DriverGuid);
            root.SetProperty(NetworkPresenceDriverFactory.SubnetsKey, subnets);
            root.SetProperty(NetworkPresenceDriverFactory.PollKey, 600);
            root.SetProperty(NetworkPresenceDriverFactory.AwayTimeoutKey, awayMinutes);
            root.SetProperty(NetworkPresenceDriverFactory.FullScanKey, 60);
            return root;
        }

        private PersonNodes AddPerson(NodeInstance root)
        {
            var person = CreateNodeInstance(NetworkPresenceDriverFactory.PersonTemplate);
            root.InverseThis2ParentNodeInstanceNavigation.Add(person);
            return new PersonNodes
            {
                Person = person,
                Home = ValueChild(person, new Guid("9eeb2722-c307-4179-8443-a332b0d74705")),
                LastSeen = ValueChild(person, new Guid("c0752dcc-6d27-4c19-b933-69b19c7a0ca8")),
                DevicesHome = ValueChild(person, new Guid("ffbd476d-5c1a-4069-9084-b5d674617be5")),
            };
        }

        private DeviceNodes AddDevice(PersonNodes person, string mac)
        {
            var device = CreateNodeInstance(NetworkPresenceDriverFactory.DeviceTemplate);
            device.SetProperty(NetworkPresenceDriverFactory.MacKey, mac);
            person.Person.InverseThis2ParentNodeInstanceNavigation.Add(device);
            return new DeviceNodes
            {
                Connected = ValueChild(device, new Guid("99d5e696-6eeb-4a8d-8f4d-ab9c81d16dd8")),
                Ip = ValueChild(device, new Guid("7a12c282-80f8-42b6-91a4-c42884ce5a72")),
            };
        }

        private async Task<Setup> StartAsync(NodeInstance root)
        {
            var driver = (NetworkPresenceDriver)await CreateDriver(root);
            var neighbors = new FakeNeighbors();
            var prober = new RecordingProber();
            driver.NeighborSource = neighbors;
            driver.Prober = prober;
            driver.SettleTime = TimeSpan.Zero;
            driver.ResolveHostName = (ip, _) => Task.FromResult(ip.ToString() == "192.168.1.23" ? "pixel-8" : null);
            await driver.Init();
            return new Setup { Driver = driver, Neighbors = neighbors, Prober = prober, Root = root };
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private static NeighborEntry Seen(string ip, string mac) => new(IPAddress.Parse(ip), mac, true);

        [Fact]
        public async Task Apply_PhoneOnTheNetwork_PersonIsHome()
        {
            var root = CreateRoot();
            var anyone = ValueChild(root, new Guid("3b2a6c04-2f77-48f1-8596-17a297c776f5"));
            var count = ValueChild(root, new Guid("2b7703e8-a178-4fd3-b4fa-be3b0ffc6440"));
            var person = AddPerson(root);
            var phone = AddDevice(person, PhoneA.ToLowerInvariant().Replace(':', '-'));
            var setup = await StartAsync(root);

            setup.Driver.Apply(new[] { Seen("192.168.1.23", PhoneA) }, T0);

            Assert.Equal(true, Value(phone.Connected));
            Assert.Equal("192.168.1.23", Value(phone.Ip));
            Assert.Equal(true, Value(person.Home));
            Assert.Equal(1, Value(person.DevicesHome));
            Assert.Equal(T0.UtcDateTime, Value(person.LastSeen));
            Assert.Equal(true, Value(anyone));
            Assert.Equal(1, Value(count));
        }

        [Fact]
        public async Task Apply_PhoneGone_StaysHomeUntilTheAwayTimeout()
        {
            var root = CreateRoot(awayMinutes: 10);
            var person = AddPerson(root);
            var phone = AddDevice(person, PhoneA);
            var setup = await StartAsync(root);

            setup.Driver.Apply(new[] { Seen("192.168.1.23", PhoneA) }, T0);
            setup.Driver.Apply(Array.Empty<NeighborEntry>(), T0.AddMinutes(9));
            Assert.Equal(true, Value(person.Home));

            setup.Driver.Apply(Array.Empty<NeighborEntry>(), T0.AddMinutes(10).AddSeconds(1));

            Assert.Equal(false, Value(phone.Connected));
            Assert.Equal(false, Value(person.Home));
            Assert.Equal(0, Value(person.DevicesHome));
            // the last seen time stays
            Assert.Equal(T0.UtcDateTime, Value(person.LastSeen));
        }

        [Fact]
        public async Task Apply_IncompleteEntry_DoesNotCount()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            AddDevice(person, PhoneA);
            var setup = await StartAsync(root);

            setup.Driver.Apply(new[] { new NeighborEntry(IPAddress.Parse("192.168.1.23"), PhoneA, false) }, T0);

            Assert.Equal(false, Value(person.Home));
        }

        [Fact]
        public async Task Apply_TwoPeople_CountsOnlyThoseHome()
        {
            var root = CreateRoot();
            var count = ValueChild(root, new Guid("2b7703e8-a178-4fd3-b4fa-be3b0ffc6440"));
            var anna = AddPerson(root);
            AddDevice(anna, PhoneA);
            var ben = AddPerson(root);
            AddDevice(ben, PhoneB);
            var setup = await StartAsync(root);

            setup.Driver.Apply(new[] { Seen("192.168.1.23", PhoneA) }, T0);

            Assert.Equal(true, Value(anna.Home));
            Assert.Equal(false, Value(ben.Home));
            Assert.Equal(1, Value(count));
        }

        [Fact]
        public async Task Apply_PersonWithTwoPhones_HomeWhileOneIsConnected()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            AddDevice(person, PhoneA);
            AddDevice(person, PhoneB);
            var setup = await StartAsync(root);

            setup.Driver.Apply(new[] { Seen("192.168.1.23", PhoneA), Seen("192.168.1.41", PhoneB) }, T0);
            Assert.Equal(2, Value(person.DevicesHome));

            setup.Driver.Apply(new[] { Seen("192.168.1.41", PhoneB) }, T0.AddMinutes(30));

            Assert.Equal(true, Value(person.Home));
            Assert.Equal(1, Value(person.DevicesHome));
        }

        [Fact]
        public async Task Apply_InvalidMac_NeverSeen()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            var phone = AddDevice(person, "not a mac");
            var setup = await StartAsync(root);

            setup.Driver.Apply(new[] { Seen("192.168.1.23", PhoneA) }, T0);

            Assert.Equal(false, Value(phone.Connected));
            Assert.Equal(false, Value(person.Home));
        }

        [Fact]
        public async Task Apply_PhoneChangesAddress_ReportsTheNewOne()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            var phone = AddDevice(person, PhoneA);
            var setup = await StartAsync(root);

            setup.Driver.Apply(new[] { Seen("192.168.1.23", PhoneA) }, T0);
            setup.Driver.Apply(new[] { Seen("192.168.1.99", PhoneA) }, T0.AddMinutes(1));

            Assert.Equal("192.168.1.99", Value(phone.Ip));
        }

        [Fact]
        public async Task Sweep_First_ProbesTheWholeNetwork_ThenOnlyKnownAddresses()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            AddDevice(person, PhoneA);
            var setup = await StartAsync(root);
            setup.Neighbors.Entries.Add(Seen("192.168.1.23", PhoneA));

            await setup.Driver.SweepAsync(true);
            await setup.Driver.SweepAsync(false);

            Assert.Equal(2, setup.Prober.Calls.Count);
            Assert.Equal(254, setup.Prober.Calls[0].Count);
            Assert.Equal(new[] { IPAddress.Parse("192.168.1.23") }, setup.Prober.Calls[1]);
        }

        [Fact]
        public async Task Sweep_DeviceNeverSeen_KeepsSweepingTheWholeNetwork()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            AddDevice(person, PhoneA);
            var setup = await StartAsync(root);

            await setup.Driver.SweepAsync(true);
            await setup.Driver.SweepAsync(false);

            Assert.All(setup.Prober.Calls, c => Assert.Equal(254, c.Count));
        }

        [Fact]
        public async Task Start_FirstSweepRunsAndFailureDoesNotStopTheDriver()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            AddDevice(person, PhoneA);
            var setup = await StartAsync(root);
            setup.Neighbors.Entries.Add(Seen("192.168.1.23", PhoneA));

            Assert.True(await setup.Driver.Start());
            Assert.Equal(true, Value(person.Home));
            await setup.Driver.Stop();

            var broken = await StartAsync(CreateRoot());
            broken.Driver.NeighborSource = new ThrowingNeighbors();
            Assert.True(await broken.Driver.Start());
            await broken.Driver.Stop();
        }

        private sealed class ThrowingNeighbors : INeighborSource
        {
            public Task<IReadOnlyList<NeighborEntry>> ReadAsync(CancellationToken token = default) => throw new System.IO.IOException("no arp table");
        }

        [Fact]
        public async Task Scan_ListsUnassignedDevicesInTheNetworkWithNames()
        {
            var root = CreateRoot();
            var anna = AddPerson(root);
            AddDevice(anna, PhoneB);
            var ben = AddPerson(root);
            var setup = await StartAsync(root);
            setup.Neighbors.Entries.AddRange(new[]
            {
                Seen("192.168.1.41", PhoneB), // assigned to Anna
                Seen("192.168.1.23", PhoneA),
                Seen("192.168.1.7", "A4:2B:B0:11:22:33"),
                Seen("10.9.9.9", "A4:2B:B0:44:55:66"), // other network (e.g. docker bridge)
                new NeighborEntry(IPAddress.Parse("192.168.1.50"), null, false),
            });

            var personDriver = setup.Driver.Persons.Single(p => p.DriverContext.NodeInstance == ben.Person);
            var found = await personDriver.Scan();

            Assert.Equal(2, found.Count);
            Assert.All(found, n => Assert.Equal(NetworkPresenceDriverFactory.DeviceTemplate, n.This2NodeTemplate));
            Assert.Equal("192.168.1.7 (A4:2B:B0:11:22:33)", found[0].Name);
            Assert.Equal("A4:2B:B0:11:22:33", found[0].GetProperty(NetworkPresenceDriverFactory.MacKey).ValueString);
            Assert.Equal("pixel-8 (192.168.1.23)", found[1].Name);
            Assert.Equal(PhoneA, found[1].GetProperty(NetworkPresenceDriverFactory.MacKey).ValueString);
            Assert.All(found, n => Assert.Equal(new[] { "connected", "ip", "last_seen" }, n.InverseThis2ParentNodeInstanceNavigation.Select(c => c.This2NodeTemplateNavigation.Key)));
        }

        [Fact]
        public async Task Configure_CreatesPersonsDevicesAndValues()
        {
            var root = CreateRoot();
            var person = AddPerson(root);
            AddDevice(person, PhoneA);
            var setup = await StartAsync(root);

            var personNode = Assert.Single(setup.Driver.Persons);
            var device = Assert.Single(personNode.Devices);
            Assert.Equal(PhoneA, device.Mac);
            Assert.Equal(4, personNode.Children.Count); // 3 values + 1 device
            Assert.Equal(2, device.Children.Count);
        }
    }
}
