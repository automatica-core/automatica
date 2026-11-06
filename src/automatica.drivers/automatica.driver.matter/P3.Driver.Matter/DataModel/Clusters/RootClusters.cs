using System.Net.NetworkInformation;
using System.Net.Sockets;
using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.SecureChannel;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.DataModel.Clusters
{
    public sealed class DescriptorCluster : ClusterServer
    {
        public const uint Id = 0x001D;
        public const uint AttributeDeviceTypeList = 0;
        public const uint AttributeServerList = 1;
        public const uint AttributeClientList = 2;
        public const uint AttributePartsList = 3;

        public DescriptorCluster() : base(Id, 2)
        {
            Attribute(AttributeDeviceTypeList, (w, t, _) =>
            {
                w.StartArray(t);
                foreach (var dt in Endpoint.DeviceTypes)
                {
                    w.StartStructure();
                    w.PutUInt(0, dt.Id);
                    w.PutUInt(1, dt.Revision);
                    w.EndContainer();
                }

                w.EndContainer();
            });
            Attribute(AttributeServerList, (w, t, _) =>
            {
                w.StartArray(t);
                foreach (var c in Endpoint.Clusters)
                {
                    w.PutUInt(null, c.ClusterId);
                }

                w.EndContainer();
            });
            Attribute(AttributeClientList, (w, t, _) =>
            {
                w.StartArray(t);
                w.EndContainer();
            });
            Attribute(AttributePartsList, (w, t, _) =>
            {
                w.StartArray(t);
                foreach (var p in Endpoint.Node?.Descendants(Endpoint.Id) ?? Enumerable.Empty<ushort>())
                {
                    w.PutUInt(null, p);
                }

                w.EndContainer();
            });
        }
    }

    public sealed class BasicInformationCluster : ClusterServer
    {
        public const uint Id = 0x0028;
        private readonly MatterNode _node;

        public BasicInformationCluster(MatterNode node) : base(Id, 3)
        {
            _node = node;
            var o = node.Options;
            Attribute(0x00, (w, t, _) => w.PutUInt(t, SessionParametersTlv.DataModelRevision));
            Attribute(0x01, (w, t, _) => w.PutString(t, o.VendorName));
            Attribute(0x02, (w, t, _) => w.PutUInt(t, node.Attestation.VendorId));
            Attribute(0x03, (w, t, _) => w.PutString(t, o.ProductName));
            Attribute(0x04, (w, t, _) => w.PutUInt(t, node.Attestation.ProductId));
            Attribute(0x05, (w, t, _) => w.PutString(t, node.State.NodeLabel ?? ""), (v, _, _) =>
            {
                var label = v.AsString();
                if (label.Length > 32)
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                node.State.NodeLabel = label;
                node.RequestPersist();
            }, writePrivilege: AclPrivilege.Manage);
            Attribute(0x06, (w, t, _) => w.PutString(t, node.State.Location ?? "XX"), (v, _, _) =>
            {
                var location = v.AsString();
                if (location.Length != 2)
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                node.State.Location = location;
                node.RequestPersist();
            }, writePrivilege: AclPrivilege.Administer);
            Attribute(0x07, (w, t, _) => w.PutUInt(t, o.HardwareVersion));
            Attribute(0x08, (w, t, _) => w.PutString(t, o.HardwareVersionString));
            Attribute(0x09, (w, t, _) => w.PutUInt(t, o.SoftwareVersion));
            Attribute(0x0A, (w, t, _) => w.PutString(t, o.SoftwareVersionString));
            Attribute(0x0F, (w, t, _) => w.PutString(t, o.SerialNumber ?? node.State.UniqueId[..16]));
            Attribute(0x12, (w, t, _) => w.PutString(t, node.State.UniqueId));
            Attribute(0x13, (w, t, _) =>
            {
                w.StartStructure(t);
                w.PutUInt(0, 3);
                w.PutUInt(1, 3);
                w.EndContainer();
            });
            Attribute(0x15, (w, t, _) => w.PutUInt(t, SessionParametersTlv.SpecificationVersion));
            Attribute(0x16, (w, t, _) => w.PutUInt(t, 1));
        }
    }

    public sealed class AccessControlCluster : ClusterServer
    {
        public const uint Id = 0x001F;
        public const int EntriesPerFabric = 4;
        public const int SubjectsPerEntry = 4;
        public const int TargetsPerEntry = 3;

        public AccessControlCluster(MatterNode node) : base(Id, 1)
        {
            Attribute(0x00, (w, t, ctx) =>
            {
                w.StartArray(t);
                foreach (var fabric in node.Fabrics)
                {
                    foreach (var entry in fabric.Acl)
                    {
                        if (fabric.FabricIndex == ctx.FabricIndex)
                        {
                            entry.Write(w, null);
                        }
                        else if (!ctx.IsFabricFiltered)
                        {
                            w.StartStructure();
                            w.PutUInt(0xFE, fabric.FabricIndex);
                            w.EndContainer();
                        }
                    }
                }

                w.EndContainer();
            }, (value, ctx, mode) =>
            {
                var fabric = ctx.Fabric ?? throw new MatterStatusException(StatusCode.UnsupportedAccess);
                var entries = mode == ListWriteMode.Append
                    ? fabric.Acl.Concat(new[] { AclEntry.Read(value, fabric.FabricIndex) }).ToList()
                    : value.Children.Select(e => AclEntry.Read(e, fabric.FabricIndex)).ToList();
                if (entries.Count > EntriesPerFabric)
                {
                    throw new MatterStatusException(StatusCode.ResourceExhausted);
                }

                if (entries.Any(e => e.Subjects?.Count > SubjectsPerEntry || e.Targets?.Count > TargetsPerEntry))
                {
                    throw new MatterStatusException(StatusCode.ResourceExhausted);
                }

                fabric.Acl = entries;
                node.RequestPersist();
            }, readPrivilege: AclPrivilege.Administer, writePrivilege: AclPrivilege.Administer);
            Attribute(0x02, (w, t, _) => w.PutUInt(t, SubjectsPerEntry));
            Attribute(0x03, (w, t, _) => w.PutUInt(t, TargetsPerEntry));
            Attribute(0x04, (w, t, _) => w.PutUInt(t, EntriesPerFabric));
        }
    }

    public static class CommissioningError
    {
        public const byte Ok = 0;
        public const byte ValueOutsideRange = 1;
        public const byte InvalidAuthentication = 2;
        public const byte NoFailSafe = 3;
        public const byte BusyWithOtherAdmin = 4;
    }

    public sealed class GeneralCommissioningCluster : ClusterServer
    {
        public const uint Id = 0x0030;
        private byte _regulatoryConfig = 2;

        public GeneralCommissioningCluster(MatterNode node) : base(Id, 1)
        {
            Attribute(0x00, (w, t, _) => w.PutUInt(t, node.Breadcrumb), (v, _, _) => node.Breadcrumb = v.AsUInt(), writePrivilege: AclPrivilege.Administer);
            Attribute(0x01, (w, t, _) =>
            {
                w.StartStructure(t);
                w.PutUInt(0, 60);
                w.PutUInt(1, 900);
                w.EndContainer();
            });
            Attribute(0x02, (w, t, _) => w.PutUInt(t, _regulatoryConfig));
            Attribute(0x03, (w, t, _) => w.PutUInt(t, 2));
            Attribute(0x04, (w, t, _) => w.PutBool(t, true));

            Command(0x00, (ctx, f) =>
            {
                var code = node.ArmFailSafe(ctx.Session, (ushort)f.GetUInt(0), f.GetUInt(1));
                return Task.FromResult(Response(0x01, code));
            }, 0x01, AclPrivilege.Administer);
            Command(0x02, (ctx, f) =>
            {
                var config = (byte)f.GetUInt(0);
                var country = f.GetString(1);
                if (config > 2 || country.Length != 2)
                {
                    return Task.FromResult(Response(0x03, CommissioningError.ValueOutsideRange));
                }

                _regulatoryConfig = config;
                node.Breadcrumb = f.GetUInt(2);
                MarkChanged(0x02);
                return Task.FromResult(Response(0x03, CommissioningError.Ok));
            }, 0x03, AclPrivilege.Administer);
            Command(0x04, (ctx, _) =>
            {
                var code = node.CompleteCommissioning(ctx.Session);
                return Task.FromResult(Response(0x05, code));
            }, 0x05, AclPrivilege.Administer);
        }

        private static InvokeResult Response(uint commandId, byte errorCode) => InvokeResult.Response(commandId, w =>
        {
            w.PutUInt(0, errorCode);
            w.PutString(1, "");
        });
    }

    public sealed class NetworkCommissioningCluster : ClusterServer
    {
        public const uint Id = 0x0031;
        private static readonly byte[] NetworkId = "ethernet"u8.ToArray();

        public NetworkCommissioningCluster() : base(Id, 1)
        {
            FeatureMap = 0x04;
            Attribute(0x00, (w, t, _) => w.PutUInt(t, 1), readPrivilege: AclPrivilege.Administer);
            Attribute(0x01, (w, t, _) =>
            {
                w.StartArray(t);
                w.StartStructure();
                w.PutBytes(0, NetworkId);
                w.PutBool(1, true);
                w.EndContainer();
                w.EndContainer();
            }, readPrivilege: AclPrivilege.Administer);
            Attribute(0x04, (w, t, _) => w.PutBool(t, true), (_, _, _) => { }, writePrivilege: AclPrivilege.Administer);
            Attribute(0x05, (w, t, _) => w.PutNull(t), readPrivilege: AclPrivilege.Administer);
            Attribute(0x06, (w, t, _) => w.PutNull(t), readPrivilege: AclPrivilege.Administer);
            Attribute(0x07, (w, t, _) => w.PutNull(t), readPrivilege: AclPrivilege.Administer);
        }
    }

    public sealed class GeneralDiagnosticsCluster : ClusterServer
    {
        public const uint Id = 0x0033;

        public GeneralDiagnosticsCluster(MatterNode node) : base(Id, 2)
        {
            Attribute(0x00, (w, t, _) => WriteInterfaces(w, t));
            Attribute(0x01, (w, t, _) => w.PutUInt(t, node.State.RebootCount));
            Attribute(0x02, (w, t, _) => w.PutUInt(t, (ulong)(DateTime.UtcNow - node.StartedAt).TotalSeconds));
            Attribute(0x08, (w, t, _) => w.PutBool(t, false));

            Command(0x00, (_, _) => Task.FromResult(InvokeResult.Error(StatusCode.ConstraintError)), privilege: AclPrivilege.Manage);
            Command(0x01, (_, _) => Task.FromResult(InvokeResult.Response(0x02, w =>
            {
                w.PutUInt(0, (ulong)(DateTime.UtcNow - node.StartedAt).TotalMilliseconds);
                w.PutUInt(1, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            })), 0x02);
        }

        private static void WriteInterfaces(TlvWriter w, byte? tag)
        {
            w.StartArray(tag);
            NetworkInterface[] interfaces;
            try
            {
                interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Take(4).ToArray();
            }
            catch (NetworkInformationException)
            {
                interfaces = Array.Empty<NetworkInterface>();
            }

            foreach (var ni in interfaces)
            {
                IPInterfaceProperties props;
                try
                {
                    props = ni.GetIPProperties();
                }
                catch (NetworkInformationException)
                {
                    continue;
                }

                var mac = ni.GetPhysicalAddress().GetAddressBytes();
                if (mac.Length is not (6 or 8))
                {
                    mac = new byte[6];
                }

                w.StartStructure();
                w.PutString(0, ni.Name.Length > 32 ? ni.Name[..32] : ni.Name);
                w.PutBool(1, true);
                w.PutNull(2);
                w.PutNull(3);
                w.PutBytes(4, mac);
                w.StartArray(5);
                foreach (var a in props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).Take(4))
                {
                    w.PutBytes(null, a.Address.GetAddressBytes());
                }

                w.EndContainer();
                w.StartArray(6);
                foreach (var a in props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6).Take(8))
                {
                    w.PutBytes(null, a.Address.GetAddressBytes());
                }

                w.EndContainer();
                w.PutUInt(7, ni.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 ? 1u : 2u);
                w.EndContainer();
            }

            w.EndContainer();
        }
    }

    public sealed class AdministratorCommissioningCluster : ClusterServer
    {
        public const uint Id = 0x003C;
        public const byte StatusBusy = 2;
        public const byte StatusPakeParameterError = 3;
        public const byte StatusWindowNotOpen = 4;

        public AdministratorCommissioningCluster(MatterNode node) : base(Id, 1)
        {
            Attribute(0x00, (w, t, _) => w.PutUInt(t, node.WindowStatus));
            Attribute(0x01, (w, t, _) => PutNullableUInt(w, t, node.AdminFabricIndex));
            Attribute(0x02, (w, t, _) => PutNullableUInt(w, t, node.AdminVendorId));

            Command(0x00, (ctx, f) =>
            {
                var status = node.OpenEnhancedCommissioningWindow(ctx.Session, (ushort)f.GetUInt(0), f.GetBytes(1), (ushort)f.GetUInt(2), (uint)f.GetUInt(3), f.GetBytes(4));
                return Task.FromResult(status.HasValue ? InvokeResult.Error(StatusCode.Failure, status) : InvokeResult.Success());
            }, privilege: AclPrivilege.Administer, timed: true);
            Command(0x02, (_, _) => Task.FromResult(node.RevokeCommissioning()
                ? InvokeResult.Success()
                : InvokeResult.Error(StatusCode.Failure, StatusWindowNotOpen)), privilege: AclPrivilege.Administer, timed: true);
        }
    }

    public static class NocStatus
    {
        public const byte Ok = 0;
        public const byte InvalidPublicKey = 1;
        public const byte InvalidNodeOpId = 2;
        public const byte InvalidNoc = 3;
        public const byte MissingCsr = 4;
        public const byte TableFull = 5;
        public const byte InvalidAdminSubject = 6;
        public const byte FabricConflict = 9;
        public const byte LabelConflict = 10;
        public const byte InvalidFabricIndex = 11;
    }

    public sealed class OperationalCredentialsCluster : ClusterServer
    {
        public const uint Id = 0x003E;

        public OperationalCredentialsCluster(MatterNode node) : base(Id, 1)
        {
            Attribute(0x00, (w, t, ctx) =>
            {
                w.StartArray(t);
                foreach (var f in node.Fabrics)
                {
                    if (f.FabricIndex == ctx.FabricIndex)
                    {
                        w.StartStructure();
                        w.PutBytes(1, f.Noc);
                        if (f.Icac != null) w.PutBytes(2, f.Icac); else w.PutNull(2);
                        w.PutUInt(0xFE, f.FabricIndex);
                        w.EndContainer();
                    }
                    else if (!ctx.IsFabricFiltered)
                    {
                        w.StartStructure();
                        w.PutUInt(0xFE, f.FabricIndex);
                        w.EndContainer();
                    }
                }

                w.EndContainer();
            }, readPrivilege: AclPrivilege.Administer);
            Attribute(0x01, (w, t, ctx) =>
            {
                w.StartArray(t);
                foreach (var f in node.Fabrics.Where(f => !ctx.IsFabricFiltered || f.FabricIndex == ctx.FabricIndex))
                {
                    w.StartStructure();
                    w.PutBytes(1, f.RootPublicKey);
                    w.PutUInt(2, f.VendorId);
                    w.PutUInt(3, f.FabricId);
                    w.PutUInt(4, f.NodeId);
                    w.PutString(5, f.Label ?? "");
                    w.PutUInt(0xFE, f.FabricIndex);
                    w.EndContainer();
                }

                w.EndContainer();
            });
            Attribute(0x02, (w, t, _) => w.PutUInt(t, MatterNode.SupportedFabrics));
            Attribute(0x03, (w, t, _) => w.PutUInt(t, (ulong)node.CommissionedFabricCount));
            Attribute(0x04, (w, t, _) =>
            {
                w.StartArray(t);
                foreach (var root in node.TrustedRoots)
                {
                    w.PutBytes(null, root);
                }

                w.EndContainer();
            });
            Attribute(0x05, (w, t, ctx) => w.PutUInt(t, ctx.FabricIndex));

            Command(0x00, (ctx, f) =>
            {
                var nonce = f.GetBytes(0);
                if (nonce.Length != 32)
                {
                    throw new MatterStatusException(StatusCode.InvalidCommand);
                }

                var elements = TlvWriter.Encode(w =>
                {
                    w.StartStructure();
                    w.PutBytes(1, node.Attestation.CertificationDeclaration);
                    w.PutBytes(2, nonce);
                    w.PutUInt(3, 0);
                    w.EndContainer();
                });
                var signature = node.Attestation.SignWithChallenge(elements, ctx.Session.AttestationChallenge);
                return Task.FromResult(InvokeResult.Response(0x01, w =>
                {
                    w.PutBytes(0, elements);
                    w.PutBytes(1, signature);
                }));
            }, 0x01, AclPrivilege.Administer);

            Command(0x02, (_, f) =>
            {
                var cert = f.GetUInt(0) switch
                {
                    1 => node.Attestation.DacCertificate,
                    2 => node.Attestation.PaiCertificate,
                    _ => throw new MatterStatusException(StatusCode.InvalidCommand)
                };
                return Task.FromResult(InvokeResult.Response(0x03, w => w.PutBytes(0, cert)));
            }, 0x03, AclPrivilege.Administer);

            Command(0x04, (ctx, f) =>
            {
                var nonce = f.GetBytes(0);
                if (nonce.Length != 32)
                {
                    throw new MatterStatusException(StatusCode.InvalidCommand);
                }

                var csr = node.CreateCsr(ctx.Session, f.GetBool(1));
                var elements = TlvWriter.Encode(w =>
                {
                    w.StartStructure();
                    w.PutBytes(1, csr);
                    w.PutBytes(2, nonce);
                    w.EndContainer();
                });
                var signature = node.Attestation.SignWithChallenge(elements, ctx.Session.AttestationChallenge);
                return Task.FromResult(InvokeResult.Response(0x05, w =>
                {
                    w.PutBytes(0, elements);
                    w.PutBytes(1, signature);
                }));
            }, 0x05, AclPrivilege.Administer);

            Command(0x06, (ctx, f) =>
            {
                var (status, index) = node.AddNoc(ctx.Session, f.GetBytes(0), f.GetBytesOrNull(1), f.GetBytes(2), f.GetUInt(3), (ushort)f.GetUInt(4));
                return Task.FromResult(NocResponse(status, index));
            }, 0x08, AclPrivilege.Administer);

            Command(0x07, (ctx, f) =>
            {
                var (status, index) = node.UpdateNoc(ctx.Session, f.GetBytes(0), f.GetBytesOrNull(1));
                return Task.FromResult(NocResponse(status, index));
            }, 0x08, AclPrivilege.Administer);

            Command(0x09, (ctx, f) =>
            {
                var (status, index) = node.UpdateFabricLabel(ctx.Session, f.GetString(0));
                return Task.FromResult(NocResponse(status, index));
            }, 0x08, AclPrivilege.Administer);

            Command(0x0A, (_, f) =>
            {
                var (status, index) = node.RemoveFabric((byte)f.GetUInt(0));
                return Task.FromResult(NocResponse(status, index));
            }, 0x08, AclPrivilege.Administer);

            Command(0x0B, (ctx, f) =>
            {
                node.AddTrustedRoot(ctx.Session, f.GetBytes(0));
                return InvokeResult.SuccessTask();
            }, privilege: AclPrivilege.Administer);
        }

        private static InvokeResult NocResponse(byte status, byte fabricIndex) => InvokeResult.Response(0x08, w =>
        {
            w.PutUInt(0, status);
            if (status == NocStatus.Ok)
            {
                w.PutUInt(1, fabricIndex);
            }
        });
    }

    public sealed class GroupKeyManagementCluster : ClusterServer
    {
        public const uint Id = 0x003F;

        public GroupKeyManagementCluster() : base(Id, 1)
        {
            Attribute(0x00, (w, t, _) =>
            {
                w.StartArray(t);
                w.EndContainer();
            }, (v, _, mode) =>
            {
                if (mode == ListWriteMode.Append || v.Children.Count > 0)
                {
                    throw new MatterStatusException(StatusCode.ResourceExhausted);
                }
            }, writePrivilege: AclPrivilege.Manage);
            Attribute(0x01, (w, t, _) =>
            {
                w.StartArray(t);
                w.EndContainer();
            });
            Attribute(0x02, (w, t, _) => w.PutUInt(t, 0));
            Attribute(0x03, (w, t, _) => w.PutUInt(t, 1));

            Command(0x00, (_, _) => Task.FromResult(InvokeResult.Error(StatusCode.ResourceExhausted)), privilege: AclPrivilege.Administer);
            Command(0x01, (_, _) => Task.FromResult(InvokeResult.Error(StatusCode.NotFound)), 0x02, AclPrivilege.Administer);
            Command(0x03, (_, f) => Task.FromResult(InvokeResult.Error(f.GetUInt(0) == 0 ? StatusCode.InvalidCommand : StatusCode.NotFound)), privilege: AclPrivilege.Administer);
            Command(0x04, (_, _) => Task.FromResult(InvokeResult.Response(0x05, w =>
            {
                w.StartArray(0);
                w.PutUInt(null, 0);
                w.EndContainer();
            })), 0x05, AclPrivilege.Administer);
        }
    }
}
