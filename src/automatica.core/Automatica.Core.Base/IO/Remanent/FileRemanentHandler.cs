using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Newtonsoft.Json;

namespace Automatica.Core.Base.IO.Remanent
{
    internal class FileRemanentHandler : IRemanentHandler
    {
        public async Task<IDictionary<Guid, DispatchValue>> GetAllAsync(CancellationToken token = default)
        {
            var dirName = Path.Combine(ServerInfo.GetBasePath(), "remanent");
            if (!Directory.Exists(dirName))
            {
                return new Dictionary<Guid, DispatchValue>();
            }

            var files = Directory.GetFiles(dirName);
            var ret = new Dictionary<Guid, DispatchValue>();

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var guid = Guid.Parse(fileInfo.Name);

                var value = await GetLastValue(guid, token);
                if (value != null)
                {
                    ret.Add(guid, value);
                }
            }

            return ret;
        }

        public async Task<DispatchValue> GetLastValue(Guid nodeInstanceId, CancellationToken token = default)
        {
            var dirName = Path.Combine(ServerInfo.GetBasePath(), "remanent");
            if (!Directory.Exists(dirName))
            {
                return null;
            }

            var valuePath = Path.Combine(dirName, $"{nodeInstanceId}");
            if (!File.Exists(valuePath))
            {
                return null;
            }

            using var reader = new StreamReader(valuePath);
            var file = await reader.ReadToEndAsync(token);

            try
            {
                var dispatchAbleValue = JsonConvert.DeserializeObject<DispatchValue>(file);
                return dispatchAbleValue;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SaveValueAsync(Guid nodeInstanceId, DispatchValue value, CancellationToken token = default)
        {
            var dirName = Path.Combine(ServerInfo.GetBasePath(), "remanent");
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            var valuePath = Path.Combine(dirName, $"{nodeInstanceId}");
            if (File.Exists(valuePath))
            {
                File.Delete(valuePath);
            }

            var jsonValue = JsonConvert.SerializeObject(value);

            await using var fileWriter = new StreamWriter(valuePath);
            await fileWriter.WriteLineAsync(jsonValue);

            fileWriter.Close();
            return true;
        }
    }
}
