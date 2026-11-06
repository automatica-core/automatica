using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Matter.Driver
{
    /// <summary>
    /// Node templates for the manually configured bridged devices: one folder per <see cref="MatterDeviceKind"/> with one
    /// child node per data point. Template ids are name based (UUID v5) so they never change as long as the keys stay.
    /// </summary>
    internal static class MatterDeviceTemplates
    {
        private static readonly Guid Namespace = new("5b1c8f0e-5b0d-4d5e-9a8c-1f6f3c2e7a10");

        public static string FolderKey(MatterDeviceKind kind) => $"matter-{kind.Key}";

        public static string PointKey(MatterDeviceKind kind, MatterDataPoint point) => $"matter-{kind.Key}.{point.Key}";

        public static Guid FolderGuid(MatterDeviceKind kind) => NameGuid($"folder/{kind.Key}");

        public static Guid PointGuid(MatterDeviceKind kind, MatterDataPoint point) => NameGuid($"point/{kind.Key}/{point.Key}");

        public static string KindNameKey(MatterDeviceKind kind) => $"MATTER_BRIDGE.DEVICE.{Upper(kind.Key)}";

        public static string PointNameKey(MatterDataPoint point) => $"MATTER_BRIDGE.POINT.{Upper(point.Key)}";

        private static string Upper(string key) => key.Replace('-', '_').ToUpperInvariant();

        /// <summary>Returns the device kind for a folder template key, or null.</summary>
        public static MatterDeviceKind KindForFolder(string templateKey) =>
            templateKey != null && templateKey.StartsWith("matter-", StringComparison.Ordinal) ? MatterDeviceKinds.Get(templateKey["matter-".Length..]) : null;

        /// <summary>Returns the data point key of a point template key of <paramref name="kind"/>, or null.</summary>
        public static string PointForTemplate(MatterDeviceKind kind, string templateKey)
        {
            var prefix = $"matter-{kind.Key}.";
            return templateKey != null && templateKey.StartsWith(prefix, StringComparison.Ordinal) ? templateKey[prefix.Length..] : null;
        }

        public static void Create(INodeTemplateFactory factory, Guid driverGuid)
        {
            foreach (var kind in MatterDeviceKinds.All)
            {
                var folder = FolderGuid(kind);
                var nameKey = KindNameKey(kind);
                factory.CreateInterfaceType(folder, $"{nameKey}.NAME", $"{nameKey}.DESCRIPTION", kind.Points.Count, int.MaxValue, false);
                factory.CreateNodeTemplate(folder, $"{nameKey}.NAME", $"{nameKey}.DESCRIPTION", FolderKey(kind), driverGuid, folder, false, false, true, false, true,
                    NodeDataType.NoAttribute, int.MaxValue, false);

                foreach (var point in kind.Points)
                {
                    var pointName = PointNameKey(point);
                    factory.CreateNodeTemplate(PointGuid(kind, point), $"{pointName}.NAME", $"{pointName}.DESCRIPTION", PointKey(kind, point), folder,
                        GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), !point.Optional,
                        point.Direction.HasFlag(MatterPointDirection.FromMatter), true,
                        point.Direction.HasFlag(MatterPointDirection.ToMatter), true,
                        ToNodeDataType(point.Type), 1, false);
                }
            }
        }

        public static NodeDataType ToNodeDataType(MatterValueType type) => type switch
        {
            MatterValueType.Boolean => NodeDataType.Boolean,
            MatterValueType.Integer => NodeDataType.Integer,
            MatterValueType.Double => NodeDataType.Double,
            _ => NodeDataType.String
        };

        /// <summary>RFC 4122 name based UUID (version 5, SHA-1).</summary>
        internal static Guid NameGuid(string name)
        {
            var ns = Namespace.ToByteArray();
            SwapByteOrder(ns);
            var hash = SHA1.HashData(ns.Concat(Encoding.UTF8.GetBytes(name)).ToArray());
            var bytes = hash[..16];
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
            SwapByteOrder(bytes);
            return new Guid(bytes);
        }

        private static void SwapByteOrder(byte[] guid)
        {
            (guid[0], guid[3]) = (guid[3], guid[0]);
            (guid[1], guid[2]) = (guid[2], guid[1]);
            (guid[4], guid[5]) = (guid[5], guid[4]);
            (guid[6], guid[7]) = (guid[7], guid[6]);
        }
    }
}
