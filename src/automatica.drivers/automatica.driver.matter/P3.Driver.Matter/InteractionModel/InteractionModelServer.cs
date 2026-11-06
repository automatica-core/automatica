using Microsoft.Extensions.Logging;
using P3.Driver.Matter.DataModel;
using P3.Driver.Matter.Exchanges;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.SecureChannel;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.InteractionModel
{
    /// <summary>The interaction model server (spec chapter 8): Read, Subscribe, Write, Invoke and Timed interactions.</summary>
    public sealed class InteractionModelServer
    {
        public const byte InteractionModelRevision = (byte)SessionParametersTlv.InteractionModelRevision;
        private const int ReportBudget = 950;
        private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(30);

        private readonly DataModelNode _node;
        private readonly ILogger _logger;

        public InteractionModelServer(DataModelNode node, ExchangeManager exchanges, SessionManager sessions, ILogger logger)
        {
            _node = node;
            _logger = logger;
            Subscriptions = new SubscriptionManager(node, exchanges, sessions, this, logger);
        }

        public SubscriptionManager Subscriptions { get; }

        public async Task HandleAsync(MessageExchange exchange, MatterMessage message)
        {
            if (exchange.SecureSession == null)
            {
                return;
            }

            try
            {
                switch (message.Opcode)
                {
                    case ImOpcode.ReadRequest:
                        await HandleReadAsync(exchange, message);
                        break;
                    case ImOpcode.SubscribeRequest:
                        await HandleSubscribeAsync(exchange, message);
                        break;
                    case ImOpcode.WriteRequest:
                        await HandleWriteAsync(exchange, message, false);
                        break;
                    case ImOpcode.InvokeRequest:
                        await HandleInvokeAsync(exchange, message, false);
                        break;
                    case ImOpcode.TimedRequest:
                        await HandleTimedAsync(exchange, message);
                        break;
                    case ImOpcode.StatusResponse:
                        break;
                    default:
                        await SendStatusAsync(exchange, StatusCode.InvalidAction);
                        break;
                }
            }
            catch (MatterStatusException e)
            {
                await SendStatusAsync(exchange, e.Status);
            }
            catch (MatterProtocolException e)
            {
                _logger.LogDebug("Invalid IM message 0x{Opcode:X2}: {Message}", message.Opcode, e.Message);
                await SendStatusAsync(exchange, StatusCode.InvalidAction);
            }
        }

        internal static Task SendStatusAsync(MessageExchange exchange, StatusCode status)
        {
            return exchange.SendAsync(ImOpcode.StatusResponse, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutUInt(0, (byte)status);
                w.PutUInt(0xFF, InteractionModelRevision);
                w.EndContainer();
            }));
        }

        private static List<AttributePath> ParsePaths(TlvElement element) =>
            element == null ? new List<AttributePath>() : element.Children.Select(AttributePath.Parse).ToList();

        private static Dictionary<(ushort, uint), uint> ParseFilters(TlvElement element)
        {
            var result = new Dictionary<(ushort, uint), uint>();
            if (element == null)
            {
                return result;
            }

            foreach (var f in element.Children)
            {
                var path = f.Require(0);
                var ep = path.GetUIntOrNull(1);
                var cl = path.GetUIntOrNull(2);
                if (ep.HasValue && cl.HasValue)
                {
                    result[((ushort)ep.Value, (uint)cl.Value)] = (uint)f.GetUInt(1);
                }
            }

            return result;
        }

        // ---------------------------------------------------------------- read / report building

        private async Task HandleReadAsync(MessageExchange exchange, MatterMessage message)
        {
            var request = TlvReader.Decode(message.Payload);
            var ctx = new AccessContext { Session = exchange.SecureSession, IsFabricFiltered = request.GetBool(3, true) };
            var reports = BuildReports(ctx, ParsePaths(request.Get(0)), ParseFilters(request.Get(4)));
            await SendReportsAsync(exchange, null, reports, true);
        }

        internal List<byte[]> BuildReports(AccessContext ctx, IReadOnlyList<AttributePath> paths, Dictionary<(ushort, uint), uint> filters, bool skipMissing = false)
        {
            var result = new List<byte[]>();
            var seen = new HashSet<(ushort, uint, uint)>();
            lock (_node.SyncRoot)
            {
                foreach (var path in paths)
                {
                    if (path.IsConcrete)
                    {
                        var ep = _node.GetEndpoint(path.Endpoint!.Value);
                        var cluster = ep?.GetCluster(path.Cluster!.Value);
                        var def = cluster?.GetAttribute(path.Attribute!.Value);
                        if (def == null)
                        {
                            if (!skipMissing)
                            {
                                var status = ep == null ? StatusCode.UnsupportedEndpoint : cluster == null ? StatusCode.UnsupportedCluster : StatusCode.UnsupportedAttribute;
                                result.Add(StatusReport(path.Endpoint.Value, path.Cluster.Value, path.Attribute.Value, status));
                            }

                            continue;
                        }

                        if (!AccessControl.Check(ctx, ep, cluster.ClusterId, def.ReadPrivilege))
                        {
                            result.Add(StatusReport(ep.Id, cluster.ClusterId, def.Id, StatusCode.UnsupportedAccess));
                            continue;
                        }

                        if (filters != null && filters.TryGetValue((ep.Id, cluster.ClusterId), out var v) && v == cluster.DataVersion)
                        {
                            continue;
                        }

                        if (seen.Add((ep.Id, cluster.ClusterId, def.Id)))
                        {
                            result.AddRange(EncodeAttribute(ctx, ep, cluster, def));
                        }

                        continue;
                    }

                    foreach (var ep in _node.Endpoints.Where(e => !path.Endpoint.HasValue || e.Id == path.Endpoint))
                    {
                        foreach (var cluster in ep.Clusters.Where(c => !path.Cluster.HasValue || c.ClusterId == path.Cluster))
                        {
                            if (filters != null && filters.TryGetValue((ep.Id, cluster.ClusterId), out var v) && v == cluster.DataVersion)
                            {
                                continue;
                            }

                            foreach (var attributeId in cluster.AttributeIds.Where(a => !path.Attribute.HasValue || a == path.Attribute))
                            {
                                var def = cluster.GetAttribute(attributeId);
                                if (def == null || !AccessControl.Check(ctx, ep, cluster.ClusterId, def.ReadPrivilege))
                                {
                                    continue;
                                }

                                if (seen.Add((ep.Id, cluster.ClusterId, attributeId)))
                                {
                                    result.AddRange(EncodeAttribute(ctx, ep, cluster, def));
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private IEnumerable<byte[]> EncodeAttribute(AccessContext ctx, Endpoint ep, ClusterServer cluster, AttributeDefinition def)
        {
            byte[] value;
            try
            {
                var w = new TlvWriter();
                def.Encode(w, null, ctx);
                value = w.ToArray();
            }
            catch (MatterStatusException e)
            {
                return new[] { StatusReport(ep.Id, cluster.ClusterId, def.Id, e.Status) };
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Encoding attribute {Endpoint}/{Cluster:X4}/{Attribute:X4} failed", ep.Id, cluster.ClusterId, def.Id);
                return new[] { StatusReport(ep.Id, cluster.ClusterId, def.Id, StatusCode.Failure) };
            }

            var single = DataReport(cluster.DataVersion, ep.Id, cluster.ClusterId, def.Id, w => w.PutRaw(Retag(value, 2)), false);
            if (single.Length <= ReportBudget || (value[0] & 0x1F) != 0x16)
            {
                return new[] { single };
            }

            // too large for one message: an empty list replaces the value, then the items are appended one by one
            var list = TlvReader.Decode(value);
            var reports = new List<byte[]>
            {
                DataReport(cluster.DataVersion, ep.Id, cluster.ClusterId, def.Id, w =>
                {
                    w.StartArray(2);
                    w.EndContainer();
                }, false)
            };
            reports.AddRange(list.Children.Select(item => DataReport(cluster.DataVersion, ep.Id, cluster.ClusterId, def.Id, w => w.PutElement(2, item), true)));
            return reports;
        }

        /// <summary>Gives an anonymously tagged encoded element a context tag.</summary>
        internal static byte[] Retag(byte[] anonymous, byte tag)
        {
            if ((anonymous[0] & 0xE0) != 0)
            {
                throw new InvalidOperationException("Element is not anonymous");
            }

            var result = new byte[anonymous.Length + 1];
            result[0] = (byte)(0x20 | anonymous[0]);
            result[1] = tag;
            anonymous.AsSpan(1).CopyTo(result.AsSpan(2));
            return result;
        }

        private static byte[] DataReport(uint dataVersion, ushort ep, uint cluster, uint attribute, Action<TlvWriter> writeData, bool append)
        {
            return TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.StartStructure(1);
                w.PutUInt(0, dataVersion);
                AttributePath.Write(w, 1, ep, cluster, attribute, append);
                writeData(w);
                w.EndContainer();
                w.EndContainer();
            });
        }

        private static byte[] StatusReport(ushort ep, uint cluster, uint attribute, StatusCode status)
        {
            return TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.StartStructure(0);
                AttributePath.Write(w, 0, ep, cluster, attribute);
                w.StartStructure(1);
                w.PutUInt(0, (byte)status);
                w.EndContainer();
                w.EndContainer();
                w.EndContainer();
            });
        }

        private static List<List<byte[]>> Chunk(List<byte[]> reports)
        {
            var chunks = new List<List<byte[]>> { new() };
            var size = 0;
            foreach (var r in reports)
            {
                if (size + r.Length > ReportBudget && chunks[^1].Count > 0)
                {
                    chunks.Add(new List<byte[]>());
                    size = 0;
                }

                chunks[^1].Add(r);
                size += r.Length;
            }

            return chunks;
        }

        /// <summary>Sends the reports as one or more ReportData messages; false if the peer did not confirm a chunk.</summary>
        internal async Task<bool> SendReportsAsync(MessageExchange exchange, uint? subscriptionId, List<byte[]> reports, bool suppressFinalResponse)
        {
            var chunks = Chunk(reports);
            for (var i = 0; i < chunks.Count; i++)
            {
                var last = i == chunks.Count - 1;
                var chunk = chunks[i];
                var payload = TlvWriter.Encode(w =>
                {
                    w.StartStructure();
                    if (subscriptionId.HasValue)
                    {
                        w.PutUInt(0, subscriptionId.Value);
                    }

                    if (chunk.Count > 0)
                    {
                        w.StartArray(1);
                        foreach (var r in chunk)
                        {
                            w.PutRaw(r);
                        }

                        w.EndContainer();
                    }

                    if (!last)
                    {
                        w.PutBool(3, true);
                    }

                    if (last && suppressFinalResponse)
                    {
                        w.PutBool(4, true);
                    }

                    w.PutUInt(0xFF, InteractionModelRevision);
                    w.EndContainer();
                });

                await exchange.SendAsync(ImOpcode.ReportData, payload);
                if (last && suppressFinalResponse)
                {
                    return true;
                }

                MatterMessage response;
                try
                {
                    response = await exchange.ReceiveAsync(ResponseTimeout);
                }
                catch (Exception e) when (e is TimeoutException or MatterProtocolException)
                {
                    return false;
                }

                if (response.Opcode != ImOpcode.StatusResponse || TlvReader.Decode(response.Payload).GetUInt(0) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        // ---------------------------------------------------------------- subscribe

        private async Task HandleSubscribeAsync(MessageExchange exchange, MatterMessage message)
        {
            var request = TlvReader.Decode(message.Payload);
            var session = exchange.SecureSession;
            var keep = request.GetBool(0);
            var floor = (ushort)request.GetUInt(1);
            var ceiling = (ushort)request.GetUInt(2);
            var paths = ParsePaths(request.Get(3));
            var hasEvents = request.Get(4)?.Children.Count > 0;
            var fabricFiltered = request.GetBool(7, true);

            if (ceiling < floor || (paths.Count == 0 && !hasEvents))
            {
                await SendStatusAsync(exchange, StatusCode.InvalidAction);
                return;
            }

            if (!keep)
            {
                Subscriptions.RemoveForPeer(session.FabricIndex, session.PeerNodeId);
            }

            var maxInterval = (ushort)Math.Max(floor, Math.Min(ceiling, (ushort)60));
            var subscription = new Subscription
            {
                Id = (uint)Random.Shared.NextInt64(1, uint.MaxValue),
                Session = session,
                Paths = paths,
                MinInterval = floor,
                MaxInterval = Math.Max(maxInterval, (ushort)1),
                FabricFiltered = fabricFiltered
            };

            Subscriptions.Add(subscription);
            var ctx = new AccessContext { Session = session, IsFabricFiltered = fabricFiltered };
            var reports = BuildReports(ctx, paths, ParseFilters(request.Get(8)));
            if (!await SendReportsAsync(exchange, subscription.Id, reports, false))
            {
                Subscriptions.Discard(subscription);
                _logger.LogDebug("Subscription priming for {Peer} failed", session);
                return;
            }

            try
            {
                await exchange.SendAsync(ImOpcode.SubscribeResponse, TlvWriter.Encode(w =>
                {
                    w.StartStructure();
                    w.PutUInt(0, subscription.Id);
                    w.PutUInt(2, subscription.MaxInterval);
                    w.PutUInt(0xFF, InteractionModelRevision);
                    w.EndContainer();
                }));
            }
            catch
            {
                Subscriptions.Discard(subscription);
                throw;
            }

            Subscriptions.Activate(subscription);
            _logger.LogInformation("Subscription {Id} from node {Node:X16} ({Paths} paths, {Min}-{Max}s)", subscription.Id, session.PeerNodeId, paths.Count, floor, subscription.MaxInterval);
        }

        // ---------------------------------------------------------------- timed

        private async Task HandleTimedAsync(MessageExchange exchange, MatterMessage message)
        {
            var timeoutMs = TlvReader.Decode(message.Payload).GetUInt(0);
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            await SendStatusAsync(exchange, StatusCode.Success);

            MatterMessage next;
            try
            {
                next = await exchange.ReceiveAsync(TimeSpan.FromMilliseconds(timeoutMs) + TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                return;
            }

            if (DateTime.UtcNow > deadline)
            {
                await SendStatusAsync(exchange, StatusCode.Timeout);
                return;
            }

            switch (next.Opcode)
            {
                case ImOpcode.WriteRequest:
                    await HandleWriteAsync(exchange, next, true);
                    break;
                case ImOpcode.InvokeRequest:
                    await HandleInvokeAsync(exchange, next, true);
                    break;
                default:
                    await SendStatusAsync(exchange, StatusCode.InvalidAction);
                    break;
            }
        }

        // ---------------------------------------------------------------- write

        private async Task HandleWriteAsync(MessageExchange exchange, MatterMessage message, bool timedInteraction)
        {
            var current = message;
            while (true)
            {
                var request = TlvReader.Decode(current.Payload);
                var suppress = request.GetBool(0);
                var timedFlag = request.GetBool(1);
                var more = request.GetBool(3);
                if (timedFlag != timedInteraction)
                {
                    await SendStatusAsync(exchange, StatusCode.TimedRequestMismatch);
                    return;
                }

                var ctx = new AccessContext { Session = exchange.SecureSession };
                var statuses = new List<(AttributePath Path, StatusCode Status)>();
                foreach (var data in request.Get(2)?.Children ?? new List<TlvElement>())
                {
                    AttributePath path;
                    try
                    {
                        path = AttributePath.Parse(data.Require(1));
                    }
                    catch (MatterStatusException)
                    {
                        continue;
                    }

                    var status = WriteAttribute(ctx, path, data.Require(2), data.GetUIntOrNull(0), timedInteraction);
                    statuses.Add((path, status));
                }

                if (!suppress)
                {
                    await exchange.SendAsync(ImOpcode.WriteResponse, TlvWriter.Encode(w =>
                    {
                        w.StartStructure();
                        w.StartArray(0);
                        foreach (var (path, status) in statuses)
                        {
                            w.StartStructure();
                            AttributePath.Write(w, 0, path.Endpoint ?? 0, path.Cluster ?? 0, path.Attribute ?? 0);
                            w.StartStructure(1);
                            w.PutUInt(0, (byte)status);
                            w.EndContainer();
                            w.EndContainer();
                        }

                        w.EndContainer();
                        w.PutUInt(0xFF, InteractionModelRevision);
                        w.EndContainer();
                    }));
                }

                if (!more)
                {
                    return;
                }

                try
                {
                    current = await exchange.ReceiveAsync(ResponseTimeout);
                }
                catch (Exception e) when (e is TimeoutException or MatterProtocolException)
                {
                    return;
                }

                if (current.Opcode != ImOpcode.WriteRequest)
                {
                    await SendStatusAsync(exchange, StatusCode.InvalidAction);
                    return;
                }
            }
        }

        private StatusCode WriteAttribute(AccessContext ctx, AttributePath path, TlvElement value, ulong? dataVersion, bool timed)
        {
            if (!path.IsConcrete)
            {
                return StatusCode.UnsupportedAttribute;
            }

            lock (_node.SyncRoot)
            {
                var ep = _node.GetEndpoint(path.Endpoint!.Value);
                if (ep == null)
                {
                    return StatusCode.UnsupportedEndpoint;
                }

                var cluster = ep.GetCluster(path.Cluster!.Value);
                if (cluster == null)
                {
                    return StatusCode.UnsupportedCluster;
                }

                var def = cluster.GetAttribute(path.Attribute!.Value);
                if (def == null)
                {
                    return StatusCode.UnsupportedAttribute;
                }

                if (!AccessControl.Check(ctx, ep, cluster.ClusterId, def.WritePrivilege))
                {
                    return StatusCode.UnsupportedAccess;
                }

                if (def.Write == null)
                {
                    return StatusCode.UnsupportedWrite;
                }

                if (def.RequiresTimedWrite && !timed)
                {
                    return StatusCode.NeedsTimedInteraction;
                }

                if (dataVersion.HasValue && dataVersion.Value != cluster.DataVersion)
                {
                    return StatusCode.DataVersionMismatch;
                }

                try
                {
                    def.Write(value, ctx, path.HasListIndex ? ListWriteMode.Append : ListWriteMode.Replace);
                }
                catch (MatterStatusException e)
                {
                    return e.Status;
                }
                catch (MatterProtocolException)
                {
                    return StatusCode.InvalidDataType;
                }
                catch (InvalidCastException)
                {
                    return StatusCode.InvalidDataType;
                }

                cluster.MarkChanged(def.Id);
                return StatusCode.Success;
            }
        }

        // ---------------------------------------------------------------- invoke

        private async Task HandleInvokeAsync(MessageExchange exchange, MatterMessage message, bool timedInteraction)
        {
            var request = TlvReader.Decode(message.Payload);
            var suppress = request.GetBool(0);
            if (request.GetBool(1) != timedInteraction)
            {
                await SendStatusAsync(exchange, StatusCode.TimedRequestMismatch);
                return;
            }

            var invokes = request.Get(2)?.Children ?? new List<TlvElement>();
            if (invokes.Count > 1)
            {
                await SendStatusAsync(exchange, StatusCode.InvalidAction);
                return;
            }

            var ctx = new InvokeContext
            {
                Access = new AccessContext { Session = exchange.SecureSession },
                Exchange = exchange,
                IsTimed = timedInteraction
            };

            var responses = new List<byte[]>();
            foreach (var invoke in invokes)
            {
                var path = CommandPath.Parse(invoke.Require(0));
                var fields = invoke.Get(1) ?? new TlvElement { Kind = TlvKind.Structure };
                var commandRef = invoke.GetUIntOrNull(2);
                var result = await InvokeAsync(ctx, path, fields);
                responses.Add(EncodeInvokeResponse(path, result, commandRef));
            }

            if (suppress)
            {
                return;
            }

            await exchange.SendAsync(ImOpcode.InvokeResponse, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBool(0, false);
                w.StartArray(1);
                foreach (var r in responses)
                {
                    w.PutRaw(r);
                }

                w.EndContainer();
                w.PutUInt(0xFF, InteractionModelRevision);
                w.EndContainer();
            }));
        }

        private async Task<InvokeResult> InvokeAsync(InvokeContext ctx, CommandPath path, TlvElement fields)
        {
            if (!path.Endpoint.HasValue)
            {
                return InvokeResult.Error(StatusCode.UnsupportedEndpoint);
            }

            CommandDefinition command;
            lock (_node.SyncRoot)
            {
                var ep = _node.GetEndpoint(path.Endpoint.Value);
                if (ep == null)
                {
                    return InvokeResult.Error(StatusCode.UnsupportedEndpoint);
                }

                var cluster = ep.GetCluster(path.Cluster);
                if (cluster == null)
                {
                    return InvokeResult.Error(StatusCode.UnsupportedCluster);
                }

                command = cluster.GetCommand(path.Command);
                if (command == null)
                {
                    return InvokeResult.Error(StatusCode.UnsupportedCommand);
                }

                if (!AccessControl.Check(ctx.Access, ep, cluster.ClusterId, command.Privilege))
                {
                    return InvokeResult.Error(StatusCode.UnsupportedAccess);
                }

                if (command.RequiresTimed && !ctx.IsTimed)
                {
                    return InvokeResult.Error(StatusCode.NeedsTimedInteraction);
                }
            }

            try
            {
                return await command.Handler(ctx, fields);
            }
            catch (MatterStatusException e)
            {
                return InvokeResult.Error(e.Status, e.ClusterStatus);
            }
            catch (MatterProtocolException e)
            {
                _logger.LogDebug("Invalid command {Endpoint}/{Cluster:X4}/{Command:X2}: {Message}", path.Endpoint, path.Cluster, path.Command, e.Message);
                return InvokeResult.Error(StatusCode.InvalidCommand);
            }
            catch (InvalidCastException)
            {
                return InvokeResult.Error(StatusCode.InvalidCommand);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Command {Endpoint}/{Cluster:X4}/{Command:X2} failed", path.Endpoint, path.Cluster, path.Command);
                return InvokeResult.Error(StatusCode.Failure);
            }
        }

        private static byte[] EncodeInvokeResponse(CommandPath path, InvokeResult result, ulong? commandRef)
        {
            var ep = path.Endpoint ?? 0;
            return TlvWriter.Encode(w =>
            {
                w.StartStructure();
                if (result.ResponseCommandId.HasValue)
                {
                    w.StartStructure(0);
                    CommandPath.Write(w, 0, ep, path.Cluster, result.ResponseCommandId.Value);
                    w.StartStructure(1);
                    result.WriteFields?.Invoke(w);
                    w.EndContainer();
                    if (commandRef.HasValue)
                    {
                        w.PutUInt(2, commandRef.Value);
                    }

                    w.EndContainer();
                }
                else
                {
                    w.StartStructure(1);
                    CommandPath.Write(w, 0, ep, path.Cluster, path.Command);
                    w.StartStructure(1);
                    w.PutUInt(0, (byte)result.Status);
                    if (result.ClusterStatus.HasValue)
                    {
                        w.PutUInt(1, result.ClusterStatus.Value);
                    }

                    w.EndContainer();
                    if (commandRef.HasValue)
                    {
                        w.PutUInt(2, commandRef.Value);
                    }

                    w.EndContainer();
                }

                w.EndContainer();
            });
        }
    }
}
