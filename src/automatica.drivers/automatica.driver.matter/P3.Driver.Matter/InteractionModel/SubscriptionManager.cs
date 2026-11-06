using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using P3.Driver.Matter.DataModel;
using P3.Driver.Matter.Exchanges;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Sessions;

namespace P3.Driver.Matter.InteractionModel
{
    public sealed class Subscription
    {
        public uint Id { get; init; }
        public SecureSession Session { get; init; }
        public List<AttributePath> Paths { get; init; }
        public ushort MinInterval { get; init; }
        public ushort MaxInterval { get; init; }
        public bool FabricFiltered { get; init; }

        internal readonly object Lock = new();
        internal readonly HashSet<(ushort, uint, uint)> Dirty = new();
        internal readonly SemaphoreSlim Signal = new(0);
        internal readonly CancellationTokenSource Cancel = new();
        internal DateTime LastReport = DateTime.UtcNow;
    }

    /// <summary>Keeps subscriptions alive and reports attribute changes (spec 8.5).</summary>
    public sealed class SubscriptionManager
    {
        public const int MaxSubscriptions = 30;

        private readonly ConcurrentDictionary<uint, Subscription> _subscriptions = new();
        private readonly DataModelNode _node;
        private readonly ExchangeManager _exchanges;
        private readonly InteractionModelServer _im;
        private readonly ILogger _logger;

        internal SubscriptionManager(DataModelNode node, ExchangeManager exchanges, SessionManager sessions, InteractionModelServer im, ILogger logger)
        {
            _node = node;
            _exchanges = exchanges;
            _im = im;
            _logger = logger;
            node.AttributeChanged += OnAttributeChanged;
            sessions.SessionRemoved += s =>
            {
                foreach (var sub in _subscriptions.Values.Where(x => x.Session == s).ToList())
                {
                    Remove(sub);
                }
            };
        }

        public int Count => _subscriptions.Count;

        public void Add(Subscription subscription)
        {
            while (_subscriptions.Count >= MaxSubscriptions)
            {
                var oldest = _subscriptions.Values.Where(s => s.Session.FabricIndex == subscription.Session.FabricIndex).OrderBy(s => s.LastReport).FirstOrDefault()
                             ?? _subscriptions.Values.OrderBy(s => s.LastReport).First();
                Remove(oldest);
            }

            // tracked from here on, so changes made while the priming report is on its way are not lost
            _subscriptions[subscription.Id] = subscription;
        }

        /// <summary>Starts reporting once the SubscribeResponse went out.</summary>
        public void Activate(Subscription subscription)
        {
            subscription.LastReport = DateTime.UtcNow;
            _ = Task.Run(() => RunAsync(subscription));
        }

        public void Discard(Subscription subscription) => Remove(subscription);

        public void RemoveForPeer(byte fabricIndex, ulong nodeId)
        {
            foreach (var s in _subscriptions.Values.Where(s => s.Session.FabricIndex == fabricIndex && s.Session.PeerNodeId == nodeId).ToList())
            {
                Remove(s);
            }
        }

        public void RemoveForFabric(byte fabricIndex)
        {
            foreach (var s in _subscriptions.Values.Where(s => s.Session.FabricIndex == fabricIndex).ToList())
            {
                Remove(s);
            }
        }

        public void RemoveAll()
        {
            foreach (var s in _subscriptions.Values.ToList())
            {
                Remove(s);
            }
        }

        private void Remove(Subscription subscription)
        {
            if (_subscriptions.TryRemove(subscription.Id, out _))
            {
                subscription.Cancel.Cancel();
                _logger.LogDebug("Subscription {Id} removed", subscription.Id);
            }
        }

        private void OnAttributeChanged(ushort endpoint, uint cluster, uint attribute)
        {
            foreach (var s in _subscriptions.Values)
            {
                if (!s.Paths.Any(p => p.Matches(endpoint, cluster, attribute)))
                {
                    continue;
                }

                lock (s.Lock)
                {
                    s.Dirty.Add((endpoint, cluster, attribute));
                }

                s.Signal.Release();
            }
        }

        private async Task RunAsync(Subscription s)
        {
            var token = s.Cancel.Token;
            var keepAlive = TimeSpan.FromSeconds(s.MaxInterval > 5 ? s.MaxInterval - 1 : s.MaxInterval);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    bool dirty;
                    lock (s.Lock)
                    {
                        dirty = s.Dirty.Count > 0;
                    }

                    var now = DateTime.UtcNow;
                    var keepAliveAt = s.LastReport + keepAlive;
                    if (dirty)
                    {
                        var earliest = s.LastReport + TimeSpan.FromSeconds(s.MinInterval);
                        if (now >= earliest)
                        {
                            if (!await FlushAsync(s))
                            {
                                break;
                            }

                            continue;
                        }

                        await Wait(s, Min(earliest, keepAliveAt) - now, token, false);
                        continue;
                    }

                    if (now >= keepAliveAt)
                    {
                        if (!await SendAsync(s, new List<byte[]>()))
                        {
                            break;
                        }

                        continue;
                    }

                    await Wait(s, keepAliveAt - now, token, true);
                }
            }
            catch (OperationCanceledException)
            {
                // removed
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Subscription {Id} failed", s.Id);
            }

            Remove(s);
        }

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        private static async Task Wait(Subscription s, TimeSpan delay, CancellationToken token, bool wakeOnSignal)
        {
            if (delay <= TimeSpan.Zero)
            {
                return;
            }

            if (wakeOnSignal)
            {
                await s.Signal.WaitAsync(delay, token);
            }
            else
            {
                await Task.Delay(delay, token);
                while (s.Signal.CurrentCount > 0)
                {
                    await s.Signal.WaitAsync(0, token);
                }
            }
        }

        private async Task<bool> FlushAsync(Subscription s)
        {
            List<(ushort Endpoint, uint Cluster, uint Attribute)> dirty;
            lock (s.Lock)
            {
                dirty = s.Dirty.ToList();
                s.Dirty.Clear();
            }

            var paths = dirty.Select(d => new AttributePath { Endpoint = d.Endpoint, Cluster = d.Cluster, Attribute = d.Attribute }).ToList();
            var ctx = new AccessContext { Session = s.Session, IsFabricFiltered = s.FabricFiltered };
            var reports = _im.BuildReports(ctx, paths, null, skipMissing: true);
            return await SendAsync(s, reports);
        }

        private async Task<bool> SendAsync(Subscription s, List<byte[]> reports)
        {
            if (s.Session.IsClosed)
            {
                return false;
            }

            var exchange = _exchanges.Initiate(s.Session, ProtocolId.InteractionModel);
            bool ok;
            try
            {
                ok = await _im.SendReportsAsync(exchange, s.Id, reports, false);
            }
            catch (MatterProtocolException)
            {
                ok = false;
            }
            finally
            {
                await exchange.CloseAsync();
            }

            s.LastReport = DateTime.UtcNow;
            if (!ok)
            {
                _logger.LogInformation("Subscription {Id} of node {Node:X16} did not answer, dropping it", s.Id, s.Session.PeerNodeId);
            }

            return ok;
        }
    }
}
