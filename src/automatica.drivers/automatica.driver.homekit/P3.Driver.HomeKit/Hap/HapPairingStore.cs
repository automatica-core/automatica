using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using P3.Driver.HomeKit.Hap.TlvData;

namespace P3.Driver.HomeKit.Hap
{
    public sealed class HapPairing
    {
        [JsonProperty("id")]
        public string Identifier { get; set; }

        [JsonProperty("ltpk")]
        public string PublicKey { get; set; }

        [JsonProperty("admin")]
        public bool IsAdmin { get; set; }
    }

    /// <summary>
    /// Holds the accessory long-term secret key, the paired controllers and the failed pair-setup counter.
    /// The controller list is persisted as JSON in the "ltpk-private" property. A plain hex string in that
    /// property is a single controller paired by an older driver version; it is bound to the first controller
    /// that proves possession of the key in pair-verify.
    /// </summary>
    public sealed class HapPairingStore
    {
        public const int MaxPairings = 16;
        public const int MaxFailedSetupAttempts = 100;

        private readonly object _lock = new object();
        private readonly List<HapPairing> _pairings = new List<HapPairing>();
        private int _failedSetupAttempts;

        public event EventHandler<PairSetupCompleteEventArgs> Changed;

        public HapPairingStore(string accessoryLtsk, string pairings)
        {
            AccessoryLtsk = accessoryLtsk;
            Load(pairings);
        }

        public string AccessoryLtsk { get; private set; }

        public bool IsPaired
        {
            get
            {
                lock (_lock)
                {
                    return _pairings.Count > 0 && !string.IsNullOrEmpty(AccessoryLtsk);
                }
            }
        }

        public bool IsLockedOut
        {
            get
            {
                lock (_lock)
                {
                    return _failedSetupAttempts >= MaxFailedSetupAttempts;
                }
            }
        }

        public int FailedSetupAttempts
        {
            get
            {
                lock (_lock)
                {
                    return _failedSetupAttempts;
                }
            }
        }

        public IReadOnlyList<HapPairing> Pairings
        {
            get
            {
                lock (_lock)
                {
                    return _pairings.Select(Clone).ToList();
                }
            }
        }

        private static HapPairing Clone(HapPairing a)
        {
            return new HapPairing { Identifier = a.Identifier, PublicKey = a.PublicKey, IsAdmin = a.IsAdmin };
        }

        private void Load(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var trimmed = value.Trim();
            if (trimmed.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<PersistedData>(trimmed);
                if (data?.Pairings != null)
                {
                    foreach (var p in data.Pairings.Where(a => !string.IsNullOrEmpty(a.PublicKey)).Take(MaxPairings))
                    {
                        _pairings.Add(p);
                    }
                }

                _failedSetupAttempts = Math.Max(0, data?.FailedSetupAttempts ?? 0);
                return;
            }

            // legacy format: hex LTPK of the single paired controller, identifier unknown
            _pairings.Add(new HapPairing { Identifier = null, PublicKey = trimmed, IsAdmin = true });
        }

        public string Serialize()
        {
            lock (_lock)
            {
                return SerializeUnlocked();
            }
        }

        private string SerializeUnlocked()
        {
            return JsonConvert.SerializeObject(new PersistedData
            {
                Pairings = _pairings.Select(Clone).ToList(),
                FailedSetupAttempts = _failedSetupAttempts
            });
        }

        /// <summary>
        /// Returns the pairing for the controller identifier. A legacy pairing without identifier is returned
        /// as a candidate; call <see cref="BindIdentifier"/> once the signature was verified.
        /// </summary>
        public HapPairing Find(string identifier)
        {
            lock (_lock)
            {
                var pairing = _pairings.FirstOrDefault(a => a.Identifier == identifier) ??
                              _pairings.FirstOrDefault(a => a.Identifier == null);
                return pairing == null ? null : Clone(pairing);
            }
        }

        public void BindIdentifier(string publicKey, string identifier)
        {
            lock (_lock)
            {
                var legacy = _pairings.FirstOrDefault(a => a.Identifier == null && a.PublicKey == publicKey);
                if (legacy == null || _pairings.Any(a => a.Identifier == identifier))
                {
                    return;
                }

                legacy.Identifier = identifier;
            }

            RaiseChanged();
        }

        public bool RegisterFailedSetupAttempt()
        {
            bool lockedOut;
            lock (_lock)
            {
                if (_failedSetupAttempts < int.MaxValue)
                {
                    _failedSetupAttempts++;
                }

                lockedOut = _failedSetupAttempts >= MaxFailedSetupAttempts;
            }

            RaiseChanged();
            return lockedOut;
        }

        /// <summary>
        /// Stores the result of a successful pair-setup. Fails if a controller is paired already.
        /// </summary>
        public bool CompletePairSetup(string accessoryLtsk, string identifier, string publicKey)
        {
            lock (_lock)
            {
                if (_pairings.Count > 0 && !string.IsNullOrEmpty(AccessoryLtsk))
                {
                    return false;
                }

                _pairings.Clear();
                AccessoryLtsk = accessoryLtsk;
                _pairings.Add(new HapPairing { Identifier = identifier, PublicKey = publicKey, IsAdmin = true });
                _failedSetupAttempts = 0;
            }

            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Add Pairing (HAP 5.10). Returns null on success, otherwise the TLV error.
        /// </summary>
        public ErrorCodes? AddPairing(string identifier, string publicKey, bool isAdmin)
        {
            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(publicKey))
            {
                return ErrorCodes.Unknown;
            }

            lock (_lock)
            {
                var existing = _pairings.FirstOrDefault(a => a.Identifier == identifier);
                if (existing != null)
                {
                    if (!string.Equals(existing.PublicKey, publicKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return ErrorCodes.Unknown;
                    }

                    existing.IsAdmin = isAdmin;
                }
                else
                {
                    if (_pairings.Count >= MaxPairings)
                    {
                        return ErrorCodes.MaxPeers;
                    }

                    _pairings.Add(new HapPairing { Identifier = identifier, PublicKey = publicKey, IsAdmin = isAdmin });
                }
            }

            RaiseChanged();
            return null;
        }

        /// <summary>
        /// Remove Pairing (HAP 5.11). Removing the last admin removes all pairings and drops the accessory LTSK,
        /// the persisted state is then the same as after <see cref="Clear"/> and the next pair-setup creates new keys.
        /// Returns the identifiers whose pairings were removed.
        /// </summary>
        public IList<string> RemovePairing(string identifier)
        {
            var removed = new List<string>();
            lock (_lock)
            {
                var existing = _pairings.FirstOrDefault(a => a.Identifier == identifier);
                if (existing == null)
                {
                    return removed;
                }

                _pairings.Remove(existing);
                removed.Add(existing.Identifier);

                if (!_pairings.Any(a => a.IsAdmin))
                {
                    removed.AddRange(_pairings.Select(a => a.Identifier));
                    _pairings.Clear();
                    AccessoryLtsk = string.Empty;
                }
            }

            RaiseChanged();
            return removed;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _pairings.Clear();
                AccessoryLtsk = string.Empty;
                _failedSetupAttempts = 0;
            }

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            string ltsk;
            string pairings;
            lock (_lock)
            {
                ltsk = AccessoryLtsk ?? string.Empty;
                pairings = SerializeUnlocked();
            }

            Changed?.Invoke(this, new PairSetupCompleteEventArgs(ltsk, pairings));
        }

        private sealed class PersistedData
        {
            [JsonProperty("v")]
            public int Version { get; set; } = 2;

            [JsonProperty("pairings")]
            public List<HapPairing> Pairings { get; set; }

            [JsonProperty("failedSetupAttempts")]
            public int FailedSetupAttempts { get; set; }
        }
    }
}
