using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using P3.Driver.EepParser.Model;

namespace P3.Driver.EepParser.Generator
{
    public static class FactoryCodeTestGenerator
    {
        public static string GenerateTests(IList<Rorg> rorgs, Dictionary<string, Tuple<long, long>> rangeRefs, string testDir)
        {
            var ret = new StringBuilder();
            foreach (var rorg in rorgs)
            {
                ret.AppendLine();
                ret.AppendLine(FactoryCodeGenerator.GeneratedHeader);
                ret.AppendLine($"//-------------------------");
                ret.AppendLine($"//-------------------------");
                ret.AppendLine($"// {rorg.Number} -------------------");
                ret.AppendLine($"//-------------------------");
                ret.AppendLine($"//-------------------------");
                ret.AppendLine();

                foreach (var func in rorg.Functions)
                {
                    foreach (var type in func.Types)
                    {
                        foreach (var dataField in type.DataFields.Values)
                        {
                            if (dataField.Guid == Guid.Empty)
                            {
                                continue;
                            }
                            var dataFieldTest = String.Format(FactoryCodeGeneratorTemplates.TestDataFieldDefaultProps,
                                rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                type.Number.Replace("0x", ""), dataField.ShortCut, dataField.Offset, dataField.Length);

                            ret.AppendLine(dataFieldTest);
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

                                var testRange = String.Format(FactoryCodeGeneratorTemplates.TestDataFieldRange,
                                    rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                    type.Number.Replace("0x", ""), dataField.ShortCut, min, max);
                                ret.AppendLine(testRange);
                            }

                            if (dataField.Scale != null)
                            {
                                var scaleTest = String.Format(CultureInfo.InvariantCulture, FactoryCodeGeneratorTemplates.TestDataFieldScale,
                                    rorg.Number.Replace("0x", ""), func.Number.Replace("0x", ""),
                                    type.Number.Replace("0x", ""), dataField.ShortCut, dataField.Scale.Min, dataField.Scale.Max);
                                ret.AppendLine(scaleTest);
                            }

                        }
                    }
                }


                var fileStr = String.Format(FactoryCodeGeneratorTemplates.TestClassTemplate,
                    rorg.Number.Replace("0x", ""), ret);


                FactoryCodeGenerator.WriteSource(Path.Combine(testDir,
                    String.Format("DriverFactoryRorg{0}Tests.cs", rorg.Number.Replace("0x", ""))), fileStr);

                ret.Clear();
            }

            return ret.ToString();
        }
    }
}
