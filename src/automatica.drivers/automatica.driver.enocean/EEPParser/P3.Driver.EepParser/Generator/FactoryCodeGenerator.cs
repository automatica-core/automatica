using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Automatica.Core.Base.Templates;
using Newtonsoft.Json.Linq;
using P3.Driver.EepParser.Model;

namespace P3.Driver.EepParser.Generator
{
    public static class FactoryCodeGenerator
    {
        private static void AddStringToJObject(string data, string translation, string code, ref JObject jobject)
        {
            var split = data.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);

            var usedJObject = jobject;
            var i = 0;
            foreach (var entry in split)
            {
                if (usedJObject.ContainsKey(entry))
                {
                    usedJObject = usedJObject[entry] as JObject;
                }
                else
                {
                    var newObj = new JObject();
                    usedJObject.Add(entry, newObj);
                    usedJObject = newObj;
                }

                i++;

                if (i == split.Length && !usedJObject.ContainsKey("NAME"))
                {
                    if (!string.IsNullOrEmpty(code))
                        usedJObject.Add("NAME", $"{new JValue(translation)} ({code.Replace("0x", "")})");
                    else
                        usedJObject.Add("NAME", $"{new JValue(translation)}");
                    usedJObject.Add("DESCRIPTION", new JValue(translation));
                }

            }
        }

        public static string GenerateCode(IList<Rorg> rorgs, Dictionary<string, Tuple<long, long>> rangeRefs, ref JObject json)
        {
            var stringBuilder = new StringBuilder();
            string corePath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

            var guidDic = new Dictionary<Guid, string>();

            foreach (var rorg in rorgs)
            {
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine($"// AUTO GENERATED {DateTime.Now}");
                stringBuilder.AppendLine("// -----------------------------------------");
                stringBuilder.AppendLine("// -----------------------------------------");
                stringBuilder.AppendLine($"// {rorg.Number} --------------------------------");
                stringBuilder.AppendLine("// -----------------------------------------");
                stringBuilder.AppendLine("// -----------------------------------------");

                var staticGuids = new List<string>();
                var funcMethods = new StringBuilder();
                var funcStringBuilder = new StringBuilder();

                var rorgGuid = new Guid($"eb2c5295-4a34-4389-8bb5-{rorg.Number.Replace("0x", "")}FFFF000000");
                guidDic.Add(rorgGuid, "");
                var rogGuidCode = String.Format(FactoryCodeGeneratorTemplates.RorgStaticGuid,
                    rorg.Number.Replace("0x", ""), rorgGuid);

                if(!staticGuids.Contains(rogGuidCode))
                    staticGuids.Add(rogGuidCode);

                foreach (var func in rorg.Functions)
                {
                    var typesMethods = new StringBuilder();
                    var typesStringBuilder = new StringBuilder();

                    var fG = new Guid($"eb2c5295-4a34-4389-8bb5-{rorg.Number.Replace("0x", "")}{func.Number.Replace("0x", "")}FF000000");
                    guidDic.Add(fG, "");
                    var funcGuidCode = String.Format(FactoryCodeGeneratorTemplates.FunctionStaticGuid,
                        rorg.Number.Replace("0x", ""),
                        func.Number.Replace("0x", ""), fG);
                    func.Guid = fG;
                    if (!staticGuids.Contains(funcGuidCode))
                        staticGuids.Add(funcGuidCode);

                    foreach (var type in func.Types)
                    {
                        var dfMethods = new StringBuilder();
                        var dfStringBuilder = new StringBuilder();

                        var g = new Guid($"eb2c5295-4a34-4389-8bb5-{rorg.Number.Replace("0x", "")}{func.Number.Replace("0x", "")}{type.OrigTypeNumber.Replace("0x", "")}000000");
                        g = GenerateNewGuid(g, type.TypeIndex, 3);
                        guidDic.Add(g, "");
                        var typeGuid = String.Format(FactoryCodeGeneratorTemplates.TypeStaticGuid,
                            rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                            type.Number.Replace("0x", ""), g);
                        if (!staticGuids.Contains(typeGuid))
                        {
                            staticGuids.Add(typeGuid);
                        }
                        else
                        {
                            continue;
                        }
                        type.Guid = g;
                        int i = 1;
                        foreach (var dataField in type.DataFields.Values)
                        {
                            var dataFieldName = String.Format(FactoryCodeGeneratorTemplates.AddDataFieldMethodName, rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                type.Number.Replace("0x", ""), dataField.ShortCut);
                            dfMethods.AppendLine($"{dataFieldName}(factory, interfaceGuid);");
                            var additionalDfProps = new StringBuilder();

                            var dfGuid = GenerateNewGuid(g, i, 2);
                            guidDic.Add(dfGuid, "");

                            var dfGuidCode = String.Format(FactoryCodeGeneratorTemplates.DataFieldStaticGuid,
                                rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                type.Number.Replace("0x", ""), dataField.ShortCut, dfGuid);

                            if (!staticGuids.Contains(dfGuidCode))
                            {
                                staticGuids.Add(dfGuidCode);
                            }
                            else
                            {
                                continue;
                            }
                            dataField.Guid = dfGuid;

                            if (dataField.Range != null)
                            {
                                long min;
                                long max;
                                if (!String.IsNullOrEmpty(dataField.Range.Ref))
                                {
                                    if (rangeRefs.ContainsKey(dataField.Range.Ref))
                                    {
                                        min = rangeRefs[dataField.Range.Ref].Item1;
                                        max = rangeRefs[dataField.Range.Ref].Item2;
                                    }
                                    else
                                    {
                                        throw new ArgumentException("ref not found");
                                    }
                                }
                                else
                                {
                                    min = dataField.Range.Min;
                                    max = dataField.Range.Max;
                                }

                                var range = String.Format(FactoryCodeGeneratorTemplates.AddRangeDataField,
                                    rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                    type.Number.Replace("0x", ""), dataField.ShortCut, min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture));
                                additionalDfProps.Append(range);
                            }
                            if (dataField.Scale != null)
                            {
                                var scale = String.Format(FactoryCodeGeneratorTemplates.AddScaleDataField,
                                    rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                    type.Number.Replace("0x", ""), dataField.ShortCut, dataField.Scale.Min.ToString(CultureInfo.InvariantCulture), dataField.Scale.Max.ToString(CultureInfo.InvariantCulture));
                                additionalDfProps.Append(scale);
                            }

                            if (!String.IsNullOrEmpty(dataField.Unit))
                            {
                                var unit = String.Format(FactoryCodeGeneratorTemplates.AddUnitDataField,
                                    rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                    type.Number.Replace("0x", ""), dataField.ShortCut, dataField.Unit);
                                additionalDfProps.Append(unit);
                            }

                            if (dataField.Enumeration != null && 
                                dataField.Enumeration.First != null && 
                                dataField.Enumeration.Second != null &&
                                dataField.Enumeration.First.Min.HasValue && 
                                dataField.Enumeration.First.Max.HasValue && 
                                dataField.Enumeration.Second.Min.HasValue && 
                                dataField.Enumeration.Second.Max.HasValue)
                            {
                                var enumeration = String.Format(FactoryCodeGeneratorTemplates.AddEnumMinMaxField,
                                    rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                    type.Number.Replace("0x", ""), dataField.ShortCut,

                                    dataField.Enumeration.First.Min.Value.ToString(CultureInfo.InvariantCulture),
                                    dataField.Enumeration.First.Max.Value.ToString(CultureInfo.InvariantCulture),
                                    dataField.Enumeration.Second.Min.Value.ToString(CultureInfo.InvariantCulture),
                                    dataField.Enumeration.Second.Max.Value.ToString(CultureInfo.InvariantCulture));
                                additionalDfProps.Append(enumeration);
                            }

                            var dataFieldCode = String.Format(FactoryCodeGeneratorTemplates.AddDataFieldTemplate,
                                rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                type.Number.Replace("0x", ""), dataField.ShortCut, dataField.Offset, dataField.Length,
                                additionalDfProps, dataField.ShortCut == "LRNB" ? "false" : "true", GetDataFieldValueType(dataField));
                            dfStringBuilder.Append(dataFieldCode);

                            var dataFieldJsonName = String.Format(FactoryCodeGeneratorTemplates.DataFieldName, rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                type.Number.Replace("0x", ""), dataField.ShortCut);
                            AddStringToJObject(dataFieldJsonName, dataField.Data, null, ref json);

                            i++;

                        }


                        var typeName = String.Format(FactoryCodeGeneratorTemplates.AddTypesMethodName, rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                            type.Number.Replace("0x", ""));
                        typesMethods.AppendLine($"{typeName}(factory, interfaceGuid, enoceanFactory);");


                        var typeJsonName = String.Format(FactoryCodeGeneratorTemplates.TypeName, rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                            type.Number.Replace("0x", ""));
                        AddStringToJObject(typeJsonName, type.Title, type.Number, ref json);

                        var typeCode = String.Format(FactoryCodeGeneratorTemplates.AddTypesTemplate, rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""), type.Number.Replace("0x", ""), dfMethods);
                        typesStringBuilder.Append(typeCode);
                        typesStringBuilder.Append(dfStringBuilder);
                    }


                    var funcName = String.Format(FactoryCodeGeneratorTemplates.AddFunctionMethodName, rorg.Number.Replace("0x", ""),
                        func.Number.Replace("0x", ""));
                    funcMethods.AppendLine($"{funcName}(factory, interfaceGuid, enoceanFactory);");


                    var funcJsonName = String.Format(FactoryCodeGeneratorTemplates.FunctionName, rorg.Number.Replace("0x", ""),
                        func.Number.Replace("0x", ""));

                    AddStringToJObject(funcJsonName, func.Title, func.Number, ref json);

                    var funcCode = String.Format(FactoryCodeGeneratorTemplates.AddFunctionTemplate, rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""), typesMethods);
                    funcStringBuilder.Append(funcCode);
                    funcStringBuilder.Append(typesStringBuilder);
                }
                var rorgCode = String.Format(FactoryCodeGeneratorTemplates.AddRorgTemplate, rorg.Number.Replace("0x", ""), funcMethods);



                stringBuilder.Append(String.Join(Environment.NewLine, staticGuids));
                rorg.Guid = rorgGuid;
                stringBuilder.Append(rorgCode);
                stringBuilder.Append(funcStringBuilder);

                var rorgName = String.Format(FactoryCodeGeneratorTemplates.RorgName,
                    rorg.Number.Replace("0x", ""));

                AddStringToJObject(rorgName, rorg.Title, rorg.Number, ref json);


                var fileStr = String.Format(FactoryCodeGeneratorTemplates.ClassNameTemplate,
                    rorg.Number.Replace("0x", ""), stringBuilder);


                using (StreamWriter sw = new StreamWriter(Path.Combine(corePath,
                    String.Format("EnOceanRorg{0}Data.cs", rorg.Number.Replace("0x", "")))))
                {
                    sw.Write(fileStr);
                }

                stringBuilder.Clear();
            }
            var str = stringBuilder.ToString();
            stringBuilder.Clear();
            stringBuilder.Append(str);


            return stringBuilder.ToString();
        }

        private static string GetDataFieldValueType(DataField dataField)
        {
            if (dataField.Scale != null)
            {
                return "NodeDataType.Double";
            }
            if (dataField.Length == 1)
            {
                return "NodeDataType.Boolean";
            }
            return "NodeDataType.Integer";
        }

        private static Guid GenerateNewGuid(Guid guid, int c, int index)
        {
            byte[] gu = guid.ToByteArray();

            gu[gu.Length - index] = (byte)(Convert.ToInt32(gu[gu.Length - index]) + c);

            return new Guid(gu);
        }
    }
}
