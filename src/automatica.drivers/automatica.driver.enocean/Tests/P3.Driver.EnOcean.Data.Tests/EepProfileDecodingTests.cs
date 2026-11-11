using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory;
using P3.Driver.EnOcean.DriverFactory.Driver;
using Xunit;

namespace P3.Driver.EnOcean.Data.Tests
{
    /// <summary>
    /// Decodes one telegram per case of every EEP profile through the driver and compares each data point with the
    /// value the EEP definition gives (EepVectors/eep-vectors.json, generated from eep.xml by generate_eep_vectors.py
    /// independently of the driver's template generator).
    /// </summary>
    public class EepProfileDecodingTests : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private const string Sender = "0581A2B3";

        public sealed class VectorField
        {
            public string Name { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public string Kind { get; set; }
            public long Raw { get; set; }
            public JsonElement Expected { get; set; }
        }

        public sealed class Vector
        {
            public string Code { get; set; }
            public int Case { get; set; }
            public string Data { get; set; }
            public List<VectorField> Fields { get; set; }
        }

        private sealed class FieldTemplate
        {
            public Guid Guid;
            public string ShortCut;
            public bool IsRaw;
            public int Offset;
            public int Length;
        }

        private sealed class TypeTemplate
        {
            public Guid Guid;
            public string Name;
            public List<FieldTemplate> Fields = new();
        }

        private static readonly Lazy<List<Vector>> Vectors = new(LoadVectors);

        private static List<Vector> LoadVectors()
        {
            using var stream = typeof(EepProfileDecodingTests).Assembly.GetManifestResourceStream("EepVectors.eep-vectors.json")
                               ?? throw new InvalidOperationException("eep-vectors.json is not embedded");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return JsonSerializer.Deserialize<List<Vector>>(reader.ReadToEnd(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public static IEnumerable<object[]> Profiles => Vectors.Value.Select(v => v.Code).Distinct().Select(c => new object[] { c });

        private static readonly Type[] RorgClasses =
            { typeof(EnOceanRorgF6Data), typeof(EnOceanRorgD5Data), typeof(EnOceanRorgA5Data), typeof(EnOceanRorgD2Data) };

        private static IEnumerable<(string Name, Guid Guid)> GuidFields(Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Guid))
                .Select(f => (f.Name, (Guid)f.GetValue(null)));

        /// <summary>
        /// All type templates of a profile code (one per EEP case), with the bit position of every data field.
        /// </summary>
        private List<TypeTemplate> TypeTemplates(string code)
        {
            var parts = code.Split('-');
            var rorgClass = RorgClasses.Single(c => c.Name == $"EnOceanRorg{parts[0]}Data");
            var fields = GuidFields(rorgClass).ToList();
            var typeRegex = new Regex($"^Type{parts[0]}_{parts[1]}_{parts[2]}_(\\d+)Guid$");

            var result = new List<TypeTemplate>();
            foreach (var (name, guid) in fields)
            {
                var match = typeRegex.Match(name);
                if (!match.Success)
                {
                    continue;
                }

                var prefix = $"DataField{parts[0]}_{parts[1]}_{parts[2]}_{match.Groups[1].Value}_";
                var type = new TypeTemplate { Guid = guid, Name = name };
                // the teach-in data point of an actuator command is not a field of the telegram
                foreach (var (dfName, dfGuid) in fields.Where(f => f.Name.StartsWith(prefix) && f.Name.EndsWith("_Guid") && !f.Name.EndsWith("_TEACHIN_Guid")))
                {
                    var template = Factory.GetNodeTemplateById(dfGuid);
                    var shortCut = dfName.Substring(prefix.Length, dfName.Length - prefix.Length - "_Guid".Length);
                    type.Fields.Add(new FieldTemplate
                    {
                        Guid = dfGuid,
                        ShortCut = shortCut,
                        IsRaw = shortCut.EndsWith("_RAW"),
                        Offset = Convert.ToInt32(template.PropertyTemplate.Single(p => p.Key == "enocean-bitoffset").DefaultValue),
                        Length = Convert.ToInt32(template.PropertyTemplate.Single(p => p.Key == "enocean-length").DefaultValue)
                    });
                }

                result.Add(type);
            }

            return result;
        }

        /// <summary>
        /// The type template of the case: its conditions accept the telegram and its fields fit the case layout best.
        /// </summary>
        private static TypeTemplate MatchCase(List<TypeTemplate> types, Vector vector, byte[] data)
        {
            var layout = vector.Fields.Select(f => (f.Offset, f.Length)).ToHashSet();
            int Score(TypeTemplate t)
            {
                var fields = t.Fields.Where(f => !f.IsRaw).Select(f => (f.Offset, f.Length)).ToHashSet();
                return fields.Count(layout.Contains) - fields.Count(f => !layout.Contains(f));
            }

            var candidates = types.Where(t => EnOceanEepMetadata.Matches(EnOceanEepMetadata.GetConditions(t.Guid), data)).ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            // cases with the same layout and conditions (D2-31-00 cases 1..3 all are CMD 6) are told apart by the
            // case order, the templates are generated in that order
            var best = candidates.Max(Score);
            var top = candidates.Where(t => Score(t) == best).ToList();
            var byOrder = vector.Case <= types.Count ? types[vector.Case - 1] : null;
            return top.Contains(byOrder) ? byOrder : top[0];
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public async Task Profile_DecodesEveryCaseAsTheEepDefines(string code)
        {
            var types = TypeTemplates(code);
            Assert.True(types.Count > 0, $"{code}: no type template generated");

            var errors = new List<string>();
            foreach (var vector in Vectors.Value.Where(v => v.Code == code))
            {
                await Decode(vector, types, errors);
            }

            Assert.True(errors.Count == 0, $"{code}:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        private async Task Decode(Vector vector, List<TypeTemplate> types, List<string> errors)
        {
            var data = Convert.FromHexString(vector.Data);
            var type = MatchCase(types, vector, data);
            var where = $"case {vector.Case} ({vector.Data})";
            if (type == null)
            {
                errors.Add($"{where}: no type template accepts the telegram");
                return;
            }

            await Dispatcher.ClearValues();

            var parts = vector.Code.Split('-');
            var rorgClass = RorgClasses.Single(c => c.Name == $"EnOceanRorg{parts[0]}Data");
            var rorgGuid = (Guid)rorgClass.GetField($"Rorg{parts[0]}Guid")!.GetValue(null)!;
            var funcGuid = (Guid)rorgClass.GetField($"Function{parts[0]}_{parts[1]}Guid")!.GetValue(null)!;

            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);
            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);
            var rorg = CreateNodeInstance(rorgGuid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(rorg);
            var func = CreateNodeInstance(funcGuid);
            rorg.InverseThis2ParentNodeInstanceNavigation.Add(func);
            var typeNode = CreateNodeInstance(type.Guid);
            typeNode.SetProperty("enocean-serialnumber", Sender);
            func.InverseThis2ParentNodeInstanceNavigation.Add(typeNode);

            var nodes = new Dictionary<FieldTemplate, NodeInstance>();
            foreach (var field in type.Fields)
            {
                var node = CreateNodeInstance(field.Guid);
                typeNode.InverseThis2ParentNodeInstanceNavigation.Add(node);
                nodes[field] = node;
            }

            var driver = await CreateDriver<EnOceanDriver>(driverNode);
            var telegram = EepTelegrams.Radio((Rorg)Convert.ToByte(parts[0], 16), data, Sender);
            if (EnOceanEepMetadata.IsReplyCommand(type.Guid))
            {
                // a command case (A5-20 controller to actuator) is what Automatica sends, its device node ignores
                // telegrams on air: check the layout with the data points directly
                var device = driver.Children.Single().Children.Single().Children.Single().Children.Single();
                foreach (var child in device.Children.OfType<EnOceanDataNode>())
                {
                    child.TelegramReceived(telegram);
                }
            }
            else
            {
                driver.TelegramReceived(telegram);
            }
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            foreach (var expected in vector.Fields)
            {
                var templates = type.Fields.Where(f => f.Offset == expected.Offset && f.Length == expected.Length).ToList();
                var field = templates.FirstOrDefault(f => !f.IsRaw);
                var label = $"{where} {expected.Name} @{expected.Offset}/{expected.Length} ({type.Name})";
                if (field == null)
                {
                    errors.Add($"{label}: no data point template");
                    continue;
                }

                var raw = templates.FirstOrDefault(f => f.IsRaw);
                if (raw != null)
                {
                    Check(errors, label + " raw", values, nodes[raw], v => Convert.ToInt64(v) == expected.Raw, expected.Raw);
                }

                switch (expected.Kind)
                {
                    case "none":
                        break;
                    case "bool":
                    case "boolenum":
                        Check(errors, label, values, nodes[field], v => v is bool b && b == expected.Expected.GetBoolean(), expected.Expected);
                        break;
                    case "scaled":
                    case "segment":
                        var d = expected.Expected.GetDouble();
                        Check(errors, label, values, nodes[field], v => v is double x && Math.Abs(x - d) < 1e-6 * Math.Max(1, Math.Abs(d)), d);
                        break;
                    default:
                        var l = expected.Expected.GetInt64();
                        Check(errors, label, values, nodes[field], v => (v is int || v is long) && Convert.ToInt64(v) == l, l);
                        break;
                }
            }
        }

        private static void Check(List<string> errors, string label, IDictionary<Guid, DispatchValue> values, NodeInstance node,
            Func<object, bool> ok, object expected)
        {
            if (!values.TryGetValue(node.ObjId, out var value))
            {
                errors.Add($"{label}: nothing dispatched, expected {Format(expected)}");
                return;
            }

            if (!ok(value.Value))
            {
                errors.Add($"{label}: dispatched {Format(value.Value)} ({value.Value?.GetType().Name}), expected {Format(expected)}");
            }
        }

        private static string Format(object value) => Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    internal static class EepTelegrams
    {
        private static byte Crc8(IEnumerable<byte> data)
        {
            byte crc = 0;
            foreach (var b in data)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ 0x07) : (byte)(crc << 1);
                }
            }
            return crc;
        }

        /// <summary>
        /// A received ESP3 RADIO_ERP1 frame, parsed like the driver parses the serial stream.
        /// </summary>
        public static RadioErp1Packet Radio(Rorg rorg, byte[] userData, string sender, byte status = 0x00)
        {
            var erp1 = new List<byte> { (byte)rorg };
            erp1.AddRange(userData);
            erp1.AddRange(Automatica.Core.Driver.Utility.Utils.StringToByteArray(sender));
            erp1.Add(status);
            var opt = new byte[] { 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x46, 0x00 };
            var header = new byte[] { (byte)(erp1.Count >> 8), (byte)erp1.Count, (byte)opt.Length, 0x01 };

            var frame = new List<byte> { 0x55 };
            frame.AddRange(header);
            frame.Add(Crc8(header));
            frame.AddRange(erp1);
            frame.AddRange(opt);
            frame.Add(Crc8(erp1.Concat(opt)));

            return (RadioErp1Packet)EnOceanTelegramFactory.FromPacket(EnOceanPacket.Parse(frame.ToArray()));
        }
    }
}
