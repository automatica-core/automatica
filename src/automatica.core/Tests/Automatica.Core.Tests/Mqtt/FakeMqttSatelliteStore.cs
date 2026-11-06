using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;

namespace Automatica.Core.Tests.Mqtt
{
    internal sealed class FakeMqttSatelliteStore : IMqttSatelliteStore
    {
        public readonly Dictionary<string, Slave> Satellites = new();
        public readonly Dictionary<Guid, Guid> NodeSatellite = new();
        public readonly List<(Guid, string)> Updates = new();

        public Slave FindSatellite(string clientId) => Satellites.TryGetValue(clientId, out var s) ? s : null;
        public Guid? GetNodeSatellite(Guid nodeId) => NodeSatellite.TryGetValue(nodeId, out var s) ? s : null;

        public void UpdateClientKey(Guid satelliteObjId, string storedKey)
        {
            Updates.Add((satelliteObjId, storedKey));
            foreach (var s in Satellites.Values)
            {
                if (s.ObjId == satelliteObjId)
                {
                    s.ClientKey = storedKey;
                }
            }
        }
    }
}
