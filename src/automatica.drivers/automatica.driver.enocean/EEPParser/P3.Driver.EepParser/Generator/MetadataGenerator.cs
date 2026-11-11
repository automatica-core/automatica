using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using P3.Driver.EepParser.Model;

namespace P3.Driver.EepParser.Generator
{
    /// <summary>
    /// Writes EnOceanEepMetadata.Generated.cs: case conditions per type template and value semantics per data field
    /// template. The driver looks them up by template guid, so they also apply to node instances created before
    /// (node instances only carry the properties that existed when they were created).
    /// </summary>
    public static class MetadataGenerator
    {
        public static void Generate(IList<Rorg> rorgs, string outDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine(FactoryCodeGenerator.GeneratedHeader);
            sb.AppendLine("namespace P3.Driver.EnOcean.DriverFactory.Driver");
            sb.AppendLine("{");
            sb.AppendLine("    internal static partial class EnOceanEepMetadata");
            sb.AppendLine("    {");
            sb.AppendLine("        static partial void Load()");
            sb.AppendLine("        {");

            foreach (var rorg in rorgs)
            {
                var r = rorg.Number.Replace("0x", "");
                foreach (var func in rorg.Functions)
                {
                    var f = func.Number.Replace("0x", "");
                    foreach (var type in func.Types.Where(t => t.Guid != Guid.Empty))
                    {
                        var t = type.Number.Replace("0x", "");
                        var typeField = $"EnOceanRorg{r}Data.Type{r}_{f}_{t}Guid";

                        if (type.Conditions.Count > 0)
                        {
                            var conditions = string.Join(", ", type.Conditions.Select(c => $"({c.Offset}, {c.Length}, 0x{c.Value:X})"));
                            sb.AppendLine($"            Condition({typeField}, {conditions});");
                        }

                        foreach (var dataField in type.DataFields.Values.Where(d => d.Guid != Guid.Empty))
                        {
                            var dfField = $"EnOceanRorg{r}Data.DataField{r}_{f}_{t}_{dataField.ShortCut}_Guid";
                            if (dataField.Segments.Count > 0)
                            {
                                var segments = string.Join(", ", dataField.Segments.Select(s =>
                                    $"({s.RangeMin}, {s.RangeMax}, {D(s.ScaleMin)}, {D(s.ScaleMax)})"));
                                sb.AppendLine($"            Segments({dfField}, {segments});");
                            }
                            else if (dataField.BoolEnum)
                            {
                                sb.AppendLine($"            BoolEnum({dfField});");
                            }
                        }
                    }
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            FactoryCodeGenerator.WriteSource(Path.Combine(outDir, "Driver", "EnOceanEepMetadata.Generated.cs"), sb.ToString());
        }

        private static string D(decimal value) => ((double)value).ToString("R", CultureInfo.InvariantCulture) + "d";
    }
}
