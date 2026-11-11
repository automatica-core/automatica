using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using P3.Driver.EepParser.Model;

namespace P3.Driver.EepParser
{
    /// <summary>
    /// Reads the EEP xml (EnOcean Alliance format) into the generator model.
    /// Every &lt;case&gt; of a type becomes its own model type (TYPE_xx_n, n counts per function).
    /// Types without cases reference another type (&lt;ref&gt; or a "see A5-10-1B" title) and are expanded after all
    /// types of the function, so the numbering of the existing types stays stable.
    /// </summary>
    public class EepReader
    {
        private static readonly Regex SeeRef = new Regex(@"see\s+([0-9A-F]{2})-([0-9A-F]{2})-([0-9A-F]{2})", RegexOptions.IgnoreCase);
        private static readonly Regex Reserved = new Regex(@"reserved|not used", RegexOptions.IgnoreCase);

        public Dictionary<string, Tuple<long, long>> RangeRefs { get; } = new Dictionary<string, Tuple<long, long>>();

        private XElement _profile;

        public List<Rorg> Read(XElement profile)
        {
            _profile = profile;
            var rorgTypes = new List<Rorg>();

            foreach (var rorg in profile.Elements("rorg"))
            {
                var number = rorg.Element("number")!.Value.Trim();
                var rorgModel = new Rorg(number.Replace("0x", ""))
                {
                    Number = number,
                    Title = rorg.Element("title")?.Value.Trim(),
                    FullName = rorg.Element("fullname")?.Value.Trim()
                };
                rorgTypes.Add(rorgModel);

                foreach (var func in rorg.Elements("func"))
                {
                    var funcNumber = func.Element("number")!.Value.Trim();
                    var function = new Function(rorgModel, funcNumber.Replace("0x", ""))
                    {
                        Number = funcNumber,
                        Title = func.Element("title")!.Value.Trim()
                    };
                    rorgModel.Functions.Add(function);

                    var types = func.Elements("type").ToList();
                    foreach (var type in types.Where(t => t.Elements("case").Any()))
                    {
                        AddType(function, type, type.Elements("case"));
                    }

                    foreach (var type in types.Where(t => !t.Elements("case").Any()))
                    {
                        var target = ResolveReference(type);
                        if (target == null)
                        {
                            Console.WriteLine($"Ignore {number}-{funcNumber}-{type.Element("number")?.Value}: no cases and no resolvable reference");
                            continue;
                        }
                        AddType(function, type, target.Elements("case"));
                    }
                }
            }

            return rorgTypes;
        }

        private XElement ResolveReference(XElement type)
        {
            string rorg, func, typ;
            var refEl = type.Element("ref");
            if (refEl != null)
            {
                rorg = refEl.Element("rorg")?.Value.Trim();
                func = refEl.Element("func")?.Value.Trim();
                typ = refEl.Element("type")?.Value.Trim();
            }
            else
            {
                var match = SeeRef.Match(type.Element("title")?.Value ?? "");
                if (!match.Success)
                {
                    return null;
                }
                rorg = match.Groups[1].Value;
                func = match.Groups[2].Value;
                typ = match.Groups[3].Value;
            }

            static bool Same(XElement el, string hex) =>
                el != null && ParseNumber(el.Element("number")?.Value) == ParseNumber("0x" + hex.Replace("0x", ""));

            var target = _profile.Elements("rorg").Where(r => Same(r, rorg))
                .SelectMany(r => r.Elements("func")).Where(f => Same(f, func))
                .SelectMany(f => f.Elements("type")).FirstOrDefault(t => Same(t, typ));

            if (target == null || ReferenceEquals(target, type))
            {
                return null;
            }

            // D2-01-13 => D2-01-01 => D2-01-00
            return target.Elements("case").Any() ? target : ResolveReference(target);
        }

        /// <summary>
        /// D2-04 CO2: the xml says "2000 (or 5000)", the family table assigns 0..5000 ppm to types 08..1E.
        /// </summary>
        private static decimal? ScaleMaxOverride(Model.Type type, string shortCut)
        {
            if (type.Parent.Parent.Number == "0xD2" && type.Parent.Number == "0x04" && shortCut == "CO2")
            {
                var typeNumber = Convert.ToInt32(type.OrigTypeNumber.Replace("0x", ""), 16);
                return typeNumber >= 0x08 ? 5000m : 2000m;
            }

            return null;
        }

        private void AddType(Function function, XElement type, IEnumerable<XElement> cases)
        {
            var typeNumber = type.Element("number")!.Value.Trim();
            var typeTitle = type.Element("title")?.Value.Trim();
            var caseModels = new List<(Model.Type Type, List<CaseCondition> Constants)>();

            foreach (var c in cases)
            {
                var typeModel = new Model.Type(function, typeNumber.Replace("0x", ""))
                {
                    Number = typeNumber,
                    Title = typeTitle,
                    OrigTypeNumber = typeNumber
                };

                function.Types.Add(typeModel);
                typeModel.Number = $"{typeModel.Number}_{function.Types.Count}";
                typeModel.TypeIndex = function.Types.Count;
                typeModel.CaseTitle = NormalizeSpace(c.Element("title")?.Value ?? "");
                SetWriteInfo(typeModel, c);

                var constants = new List<CaseCondition>();
                foreach (var condition in c.Element("condition")?.Elements("datafield") ?? Enumerable.Empty<XElement>())
                {
                    var value = ParseNumber(condition.Element("value")?.Value);
                    if (value.HasValue)
                    {
                        var cc = new CaseCondition
                        {
                            Offset = Convert.ToInt32(condition.Element("bitoffs")!.Value.Trim()),
                            Length = Convert.ToInt32(condition.Element("bitsize")!.Value.Trim()),
                            Value = value.Value
                        };
                        typeModel.Conditions.Add(cc);
                        constants.Add(cc);
                    }
                }

                foreach (var dataField in c.Elements("datafield"))
                {
                    var constant = AddDataField(typeModel, dataField);
                    if (constant != null && !constants.Contains(constant))
                    {
                        constants.Add(constant);
                    }
                }

                caseModels.Add((typeModel, constants));
            }

            DeriveConditions(caseModels);
        }

        private static readonly Regex IncomingCase = new Regex(@"respon|reply|WindowState", RegexOptions.IgnoreCase);
        private static readonly Regex NoChange = new Regex(@"do\s*not\s*change|no\s*change|nochange|all\s+(output\s+)?channels", RegexOptions.IgnoreCase);
        private static readonly Regex LeadingNumber = new Regex(@"^\s*(0x[0-9A-F]+|0b[01]+|\d+)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Profiles Automatica can send to: D2-01 switches/dimmers, D2-05 blinds and A5-38-08 central commands. Every case
        /// of them except the device's answers (status/measurement responses, replies) is a command.
        /// </summary>
        private static bool IsWritableProfile(Model.Type type)
        {
            var rorg = type.Parent.Parent.Number;
            var func = type.Parent.Number;
            return rorg == "0xD2" && (func == "0x01" || func == "0x05") ||
                   rorg == "0xA5" && func == "0x38" && type.OrigTypeNumber == "0x08";
        }

        private static void SetWriteInfo(Model.Type type, XElement c)
        {
            if (!IsWritableProfile(type) || IncomingCase.IsMatch(type.CaseTitle))
            {
                return;
            }

            type.Outgoing = true;
            var fields = c.Elements("datafield").ToList();
            type.TelegramSize = type.Parent.Parent.Number == "0xA5"
                ? 4
                : Math.Max(1, fields.Max(f => (Convert.ToInt32(f.Element("bitoffs")?.Value.Trim() ?? "0") + Convert.ToInt32(f.Element("bitsize")?.Value.Trim() ?? "0") + 7) / 8));

            void Default(int offset, int length, long value)
            {
                type.WriteDefaults.RemoveAll(d => d.Offset == offset && d.Length == length);
                type.WriteDefaults.Add(new CaseCondition { Offset = offset, Length = length, Value = value });
            }

            foreach (var field in fields.Where(f => !f.Elements("reserved").Any()))
            {
                var offset = Convert.ToInt32(field.Element("bitoffs")!.Value.Trim());
                var length = Convert.ToInt32(field.Element("bitsize")!.Value.Trim());
                var shortCut = field.Element("shortcut")?.Value.Trim();
                if (shortCut == "LRNB" || shortCut == "LRN")
                {
                    Default(offset, length, 1); // data telegram
                    continue;
                }

                var items = field.Element("enum")?.Elements("item").ToList() ?? new List<XElement>();
                var single = items.Count == 1 ? ParseNumber(items[0].Element("value")?.Value) : null;
                if (single.HasValue)
                {
                    Default(offset, length, single.Value);
                    continue;
                }

                var keep = items.FirstOrDefault(i => i.Element("value") != null && NoChange.IsMatch(i.Element("description")?.Value ?? ""));
                var match = keep == null ? null : LeadingNumber.Match(keep.Element("value")!.Value);
                if (match != null && match.Success)
                {
                    Default(offset, length, ParseNumber(match.Groups[1].Value)!.Value);
                }
            }

            foreach (var condition in c.Element("condition")?.Elements("datafield") ?? Enumerable.Empty<XElement>())
            {
                var value = ParseNumber(condition.Element("value")?.Value);
                if (value.HasValue)
                {
                    Default(Convert.ToInt32(condition.Element("bitoffs")!.Value.Trim()), Convert.ToInt32(condition.Element("bitsize")!.Value.Trim()), value.Value);
                }
            }

            type.WriteDefaults.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        }

        /// <summary>
        /// Most multi-case VLD profiles mark the message type only as a constant field (e.g. D2-01 CMD = 0x4, D2-10 MID = 2).
        /// A constant is used as condition if another case of the type has a different constant at the same bits.
        /// </summary>
        private static void DeriveConditions(List<(Model.Type Type, List<CaseCondition> Constants)> cases)
        {
            if (cases.Count < 2)
            {
                return;
            }

            foreach (var (type, constants) in cases)
            {
                if (type.Conditions.Count > 0)
                {
                    continue;
                }

                foreach (var constant in constants)
                {
                    var discriminates = cases.Any(other => !ReferenceEquals(other.Type, type) &&
                        other.Constants.Any(o => o.Offset == constant.Offset && o.Length == constant.Length && o.Value != constant.Value));
                    if (discriminates)
                    {
                        type.Conditions.Add(constant);
                    }
                }
            }
        }

        /// <returns>the field as constant if its enum has exactly one fixed value</returns>
        private CaseCondition AddDataField(Model.Type typeModel, XElement dataField)
        {
            if (dataField.Elements("reserved").Any())
            {
                return null;
            }

            var offset = Convert.ToInt32(dataField.Element("bitoffs")!.Value.Trim());
            var size = Convert.ToInt32(dataField.Element("bitsize")!.Value.Trim());

            var data = dataField.Element("data")?.Value.Trim();
            var shortCutEl = dataField.Element("shortcut");
            // an empty <shortcut/> stays empty like it always did (A5-20-03), only a missing one is derived
            var shortCut = shortCutEl != null
                ? shortCutEl.Value.Trim().Replace("(", "_").Replace(".", "_").Replace(")", "_").Replace("-", "_").Replace("/", "").Replace(" ", "")
                : DeriveShortCut(data);

            var unit = dataField.Element("unit");
            var range = dataField.Element("range");
            var scale = dataField.Element("scale");
            var enumeration = dataField.Element("enum");

            var dataFieldModel = new DataField(typeModel)
            {
                Offset = offset,
                Length = size,
                Data = string.IsNullOrEmpty(data) ? shortCut : NormalizeSpace(data),
                Description = NormalizeSpace(dataField.Element("description")?.Value ?? ""),
                ShortCut = shortCut,
                Unit = string.IsNullOrWhiteSpace(unit?.Value) ? null : unit.Value.Trim()
            };

            CaseCondition constant = null;

            try
            {
                if (range != null)
                {
                    dataFieldModel.Range = new Model.Range();

                    var minEl = range.Element("min");
                    var maxEl = range.Element("max");

                    if (minEl != null && maxEl != null)
                    {
                        var min = ParseNumber(minEl.Value)!.Value;
                        var max = ParseNumber(maxEl.Value)!.Value;

                        RangeRefs.TryAdd(shortCutEl?.Value ?? shortCut, new Tuple<long, long>(min, max));
                        dataFieldModel.Range.Min = min;
                        dataFieldModel.Range.Max = max;
                    }
                    else
                    {
                        dataFieldModel.Range.Ref = range.Element("ref")!.Value;
                    }
                }
                if (scale != null)
                {
                    // assigned before parsing: an unparsable scale ("4095 (409,5)") stays a 0..0 scale (double, raw value)
                    dataFieldModel.Scale = new Scale();

                    var minEl = scale.Element("min");
                    var maxEl = scale.Element("max");

                    if (minEl != null && maxEl != null)
                    {
                        dataFieldModel.Scale.Min = ParseDecimal(minEl.Value);
                        dataFieldModel.Scale.Max = ScaleMaxOverride(typeModel, shortCut) ?? ParseDecimal(maxEl.Value);
                    }
                }

                if (enumeration != null)
                {
                    var items = enumeration.Elements("item").ToList();
                    var enumerationItem = new Enumeration();

                    foreach (var item in items)
                    {
                        var enumItem = new EnumerationItem();

                        if (enumerationItem.First == null)
                        {
                            enumerationItem.First = enumItem;
                        }
                        else
                        {
                            enumerationItem.Second = enumItem;
                        }

                        var value = ParseNumber(item.Element("value")?.Value);
                        if (value.HasValue && value.Value <= int.MaxValue)
                        {
                            enumItem.Value = (int)value.Value;
                        }

                        enumItem.Min = ParseNumber(item.Element("min")?.Value);
                        enumItem.Max = ParseNumber(item.Element("max")?.Value);
                        enumItem.Description = item.Element("description")?.Value;

                        var itemScale = item.Element("scale");
                        if (itemScale != null && enumItem.Min.HasValue && enumItem.Max.HasValue &&
                            itemScale.Element("min") != null && itemScale.Element("max") != null)
                        {
                            dataFieldModel.Segments.Add(new ScaleSegment
                            {
                                RangeMin = enumItem.Min.Value,
                                RangeMax = enumItem.Max.Value,
                                ScaleMin = ParseDecimal(itemScale.Element("min")!.Value),
                                ScaleMax = ParseDecimal(itemScale.Element("max")!.Value)
                            });
                            var itemUnit = item.Element("unit")?.Value.Trim();
                            if (string.IsNullOrEmpty(dataFieldModel.Unit) && !string.IsNullOrEmpty(itemUnit))
                            {
                                dataFieldModel.Unit = itemUnit;
                            }
                        }
                    }

                    if (items.Count == 1 && enumerationItem.First?.Value != null && !enumerationItem.First.Min.HasValue)
                    {
                        constant = new CaseCondition { Offset = offset, Length = size, Value = enumerationItem.First.Value.Value };
                    }

                    // a two state enum given as two value ranges (A5-07-01 PIRS 0..127 / 128..255) is dispatched as bool
                    dataFieldModel.BoolEnum = items.Count == 2 && dataFieldModel.Segments.Count == 0 &&
                                              items.All(i => ParseNumber(i.Element("min")?.Value).HasValue && ParseNumber(i.Element("max")?.Value).HasValue) &&
                                              items.All(i => !Reserved.IsMatch(i.Element("description")?.Value ?? ""));

                    dataFieldModel.Enumeration = enumerationItem;

                    if (enumerationItem.First != null && enumerationItem.First.Min.HasValue)
                    {
                        var parent = dataFieldModel.Parent;
                        dataFieldModel.Parent = null;
                        var copy = JsonConvert.DeserializeObject<DataField>(JsonConvert.SerializeObject(dataFieldModel));
                        copy.Enumeration = null;
                        copy.Segments.Clear();
                        copy.BoolEnum = false;
                        copy.ShortCut = copy.ShortCut + "_RAW";
                        copy.Data += " raw";

                        dataFieldModel.Parent = parent;
                        copy.Parent = parent;

                        typeModel.DataFields.TryAdd(copy.ShortCut, copy);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ignore rest of datafield {typeModel.TypeId()} {dataFieldModel.ShortCut}: {e.Message}");
            }
            finally
            {
                if (!typeModel.DataFields.TryAdd(dataFieldModel.ShortCut, dataFieldModel))
                {
                    // the xml reuses a shortcut for different fields (D2-14-30 SMA x6), the first keeps the
                    // shortcut, the others get their bit offset appended
                    var unique = $"{dataFieldModel.ShortCut}_{dataFieldModel.Offset}";
                    if (typeModel.DataFields.TryAdd(unique, dataFieldModel))
                    {
                        dataFieldModel.ShortCut = unique;
                    }
                    else
                    {
                        Console.WriteLine($"Ignore datafield {typeModel.TypeId()} {dataFieldModel.ShortCut} already in list");
                    }
                }
            }

            return constant;
        }

        private static string NormalizeSpace(string value) => Regex.Replace(value, @"\s+", " ").Trim();

        /// <summary>
        /// Newer EEP profiles have no shortcut, it is derived from the data name ("Dim value" => "DIM_VALUE").
        /// </summary>
        private static string DeriveShortCut(string data)
        {
            var shortCut = Regex.Replace((data ?? "").ToUpperInvariant(), "[^A-Z0-9]+", "_").Trim('_');
            if (shortCut.Length > 32)
            {
                shortCut = shortCut.Substring(0, 32).TrimEnd('_');
            }
            if (shortCut.Length == 0 || char.IsDigit(shortCut[0]))
            {
                shortCut = "F_" + shortCut;
            }
            return shortCut;
        }

        private static decimal ParseDecimal(string value) =>
            Convert.ToDecimal(value.Trim(), CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses "12", "0x0C" and "0b1100"; null for empty values and patterns like "0b11X0XXXX" or "1...5".
        /// </summary>
        public static long? ParseNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            try
            {
                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    return long.Parse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToInt64(value.Substring(2), 2);
                }
                return long.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return null;
            }
            catch (OverflowException)
            {
                return null;
            }
        }
    }
}
