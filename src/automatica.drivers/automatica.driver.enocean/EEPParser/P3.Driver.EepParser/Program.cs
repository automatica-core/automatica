using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using P3.Driver.EepParser.Generator;
using P3.Driver.EepParser.Model;

namespace P3.Driver.EepParser
{
    static class Program
    {
        static void Main(string[] args)
        {
            string corePath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var doc = XDocument.Load(Path.Combine(corePath, "eep.xml"));

            var rorgs = doc.Root.Elements();
            var rangeRefDictionary = new Dictionary<string, Tuple<long, long>>();

            var rorgTypes = new List<Rorg>();

            var types = new List<string>();
            

            foreach (var rorg in rorgs)
            {
                var number = rorg.Element("number");
                var title = rorg.Element("title");
                var fullName = rorg.Element("fullname");

                var rorgModel = new Rorg(rorg.Element("number").Value.Replace("0x", ""))
                {
                    Number = number.Value,
                    Title = title.Value,
                    FullName = fullName.Value
                };

                rorgTypes.Add(rorgModel);
                

                foreach (var func in rorg.Elements("func"))
                {

                    var funcNumber = func.Element("number");
                    var funcTitle = func.Element("title");

                    var function = new Function(rorgModel, func.Element("number").Value.Replace("0x", ""))
                    {
                        Number = funcNumber.Value,
                        Title = funcTitle.Value
                    };

                    rorgModel.Functions.Add(function);

                
                    foreach (var type in func.Elements("type"))
                    {
                        foreach(var c in type.Elements("case"))
                        {
                            var typeNumber = type.Element("number");
                            var typeTitle = type.Element("title");


                            var typeModel = new Model.Type(function, type.Element("number").Value.Replace("0x", ""))
                            {
                                Number = typeNumber.Value,
                                Title = typeTitle.Value,
                                OrigTypeNumber = typeNumber.Value
                            };

                            function.Types.Add(typeModel);

                            var sbNumber = new StringBuilder();
                            sbNumber.Append(typeModel.Number);
                            sbNumber.Append("_");
                            sbNumber.Append(function.Types.Count);
                            typeModel.Number = sbNumber.ToString();

                            types.Add(typeModel.TypeId());
                            typeModel.TypeIndex = function.Types.Count;

                            foreach (var dataField in c.Elements("datafield"))
                            {
                                var reserved = dataField.Descendants("reserved").Count() == 1;

                                if (reserved)
                                {
                                    continue;
                                }

                                var offset = dataField.Element("bitoffs").Value;
                                var size = dataField.Element("bitsize").Value;

                                var shortCut = dataField.Element("shortcut");
                                var data = dataField.Element("data");
                                var description = dataField.Element("description");

                                var unit = dataField.Element("unit");

                                var range = dataField.Element("range");
                                var scale = dataField.Element("scale");
                                var enumeration = dataField.Element("enum");

                                var dataFieldModel = new DataField(typeModel)
                                {
                                    Offset = Convert.ToInt32(offset),
                                    Length = Convert.ToInt32(size),
                                    Data = data.Value,
                                    Description = description.Value,
                                    ShortCut = shortCut.Value.Replace("(", "_").Replace(".", "_").Replace(")", "_").Replace("-", "_").Replace("/", "").Replace(" ", ""),
                                    Unit = unit?.Value
                                };

                                try
                                {
                                    if (range != null)
                                    {
                                        dataFieldModel.Range = new P3.Driver.EepParser.Model.Range();;

                                        var minEl = range.Element("min");
                                        var maxEl = range.Element("max");

                                        if (minEl != null && maxEl != null)
                                        {
                                            long min;
                                            long max;
                                            min = minEl.Value.Contains("0x") ? long.Parse(minEl.Value.Replace("0x", ""), NumberStyles.HexNumber) : Convert.ToInt64(minEl.Value);
                                            max = maxEl.Value.Contains("0x") ? long.Parse(maxEl.Value.Replace("0x", ""), NumberStyles.HexNumber) : Convert.ToInt64(maxEl.Value);
                                           
                                            if (!rangeRefDictionary.ContainsKey(shortCut.Value))
                                            {
                                                rangeRefDictionary.Add(shortCut.Value, new Tuple<long, long>(min, max));
                                            }
                                            dataFieldModel.Range.Min = min;
                                            dataFieldModel.Range.Max = max;
                                        }
                                        else
                                        {
                                            var refEl = range.Element("ref");

                                            dataFieldModel.Range.Ref = refEl.Value;
                                        }
                                    }
                                    if (scale != null)
                                    {
                                        dataFieldModel.Scale = new Scale();

                                        var minEl = scale.Element("min");
                                        var maxEl = scale.Element("max");

                                        if (minEl != null && maxEl != null)
                                        {
                                            var min = Convert.ToDecimal(minEl.Value, CultureInfo.InvariantCulture);
                                            var max = Convert.ToDecimal(maxEl.Value, CultureInfo.InvariantCulture);

                                            dataFieldModel.Scale.Min = min;
                                            dataFieldModel.Scale.Max = max;
                                        }
                                    }

                                    if (enumeration != null)
                                    {
                                        var enumerationItem = new Enumeration();

                                        var items = enumeration.Elements("item");

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
                                            var value = item.Element("value");
                                             var min = item.Element("min");
                                            var max = item.Element("max");
                                            var enumDescription = item.Element("description");

                                            if (value != null)
                                            {
                                                try
                                                {
                                                    if (value.Value.StartsWith("0x"))
                                                    {
                                                        enumItem.Value = Convert.ToInt32(value.Value, 16);
                                                    }
                                                    else if (value.Value.Contains("..."))
                                                    {
                                                        continue;

                                                    }
                                                    else if (value.Value.StartsWith("0b"))
                                                    {
                                                        continue;

                                                    }
                                                    else if (String.IsNullOrEmpty(value.Value))
                                                    {
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        enumItem.Value = Convert.ToInt32(value.Value);
                                                    }
                                                }
                                                catch
                                                {
                                                    break;
                                                }
                                            }
                                            if (min != null)
                                            {
                                                if (min.Value.StartsWith("0x"))
                                                {
                                                    enumItem.Min = Convert.ToInt64(min.Value, 16);
                                                }
                                                else
                                                {
                                                    enumItem.Min = Convert.ToInt64(min.Value);
                                                }
                                            }
                                            if (max != null)
                                            {
                                                if (max.Value.StartsWith("0x"))
                                                {
                                                    enumItem.Max = Convert.ToInt64(max.Value, 16);
                                                }
                                                else
                                                {
                                                    enumItem.Max = Convert.ToInt64(max.Value);
                                                }
                                            }
                                            if (enumDescription != null)
                                            {
                                                enumItem.Description = enumDescription.Value;
                                            }

                                        }

                                        dataFieldModel.Enumeration = enumerationItem;


                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Ignore type {e}");
                                }
                                finally
                                {
                                    if (!typeModel.DataFields.ContainsKey(dataFieldModel.ShortCut))
                                    {
                                        typeModel.DataFields.Add(dataFieldModel.ShortCut, dataFieldModel);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Ignore datafield {dataFieldModel} already in list");
                                    }
                                }
                            }
                        }
                    }
                    
                }
                
            }


            var json = new JObject();
            var builder = FactoryCodeGenerator.GenerateCode(rorgTypes, rangeRefDictionary, ref json);
            var tests = FactoryCodeTestGenerator.GenerateTests(rorgTypes, rangeRefDictionary);

            Console.WriteLine(builder);
            Console.WriteLine(tests);

            Console.ReadLine();
        }
    }
}
