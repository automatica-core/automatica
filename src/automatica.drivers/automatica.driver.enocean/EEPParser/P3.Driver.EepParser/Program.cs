using System;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using P3.Driver.EepParser.Generator;

namespace P3.Driver.EepParser
{
    /// <summary>
    /// Generates the EnOcean node templates (EnOceanRorg*Data.cs), the runtime metadata (EnOceanEepMetadata.cs),
    /// the template tests and the english template names from eep.xml.
    ///
    /// Usage: P3.Driver.EepParser [eep.xml] [driver factory dir] [driver factory tests dir] [translations output]
    /// Template guids are pinned in eep-guids.json next to eep.xml, so existing node instances keep their templates.
    /// </summary>
    static class Program
    {
        static int Main(string[] args)
        {
            var baseDir = AppContext.BaseDirectory;
            var eepPath = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine(baseDir, "eep.xml"));
            var outDir = Path.GetFullPath(args.Length > 1 ? args[1] : baseDir);
            var testDir = Path.GetFullPath(args.Length > 2 ? args[2] : baseDir);
            var translationsPath = Path.GetFullPath(args.Length > 3 ? args[3] : Path.Combine(baseDir, "eep-translations.generated.json"));
            var guidLockPath = Path.Combine(Path.GetDirectoryName(eepPath)!, "eep-guids.json");

            var doc = XDocument.Load(eepPath);
            var reader = new EepReader();
            var rorgs = reader.Read(doc.Root);

            var guidLock = GuidLock.Load(guidLockPath);
            var json = new JObject();

            FactoryCodeGenerator.GenerateCode(rorgs, reader.RangeRefs, guidLock, outDir, ref json);
            FactoryCodeTestGenerator.GenerateTests(rorgs, reader.RangeRefs, testDir);
            MetadataGenerator.Generate(rorgs, outDir);

            guidLock.Save();
            File.WriteAllText(translationsPath, json.ToString());

            Console.WriteLine($"Generated {rorgs.Count} rorgs into {outDir}, tests into {testDir}, names into {translationsPath}");
            return 0;
        }
    }
}
