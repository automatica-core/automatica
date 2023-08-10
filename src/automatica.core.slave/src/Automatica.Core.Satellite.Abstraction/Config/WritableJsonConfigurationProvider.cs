using System.IO;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;

namespace Automatica.Core.Satellite.Abstraction.Config
{
    public class WritableJsonConfigurationProvider : JsonConfigurationProvider
    {
        public WritableJsonConfigurationProvider(JsonConfigurationSource source) : base(source)
        {
        }

        public override void Set(string key, string value)
        {
            base.Set(key, value);

            //Get Whole json file and change only passed key with passed value. It requires modification if you need to support change multi level json structure
            var sourceFileProvider = Source.FileProvider;
            if (sourceFileProvider != null)
            {
                var sourcePath = base.Source.Path;
                if (sourcePath != null)
                {
                    var fileFullPath = sourceFileProvider.GetFileInfo(sourcePath).PhysicalPath;
                    if (fileFullPath != null)
                    {
                        var json = File.ReadAllText(fileFullPath);
                        dynamic jsonObj = JsonConvert.DeserializeObject(json);
                        if (jsonObj != null)
                        {
                            jsonObj[key] = value;
                            var output = JsonConvert.SerializeObject(jsonObj, new JsonSerializerSettings
                            {
                                Formatting = Formatting.Indented
                            });
                            File.WriteAllText(fileFullPath, output);
                        }
                    }
                }
            }
        }
    }
}
