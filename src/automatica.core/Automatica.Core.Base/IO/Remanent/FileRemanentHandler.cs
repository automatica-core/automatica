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
        private const string TempFileExtension = ".tmp";

        public async Task<IDictionary<Guid, DispatchValue>> GetAllAsync(CancellationToken token = default)
        {
            var dirName = ServerInfo.GetRemanentDirectory();
            if (!Directory.Exists(dirName))
            {
                return new Dictionary<Guid, DispatchValue>();
            }

            var files = Directory.GetFiles(dirName);
            var ret = new Dictionary<Guid, DispatchValue>();

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Name.EndsWith(TempFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(file); // left over from an interrupted write
                    continue;
                }

                // ignore stray files, they must not abort the startup
                if (!Guid.TryParse(fileInfo.Name, out var guid))
                {
                    continue;
                }

                try
                {
                    var value = await GetLastValue(guid, token);
                    if (value != null)
                    {
                        ret[guid] = value;
                    }
                }
                catch (Exception) when (!token.IsCancellationRequested)
                {
                    // unreadable file, ignore the value
                }
            }

            return ret;
        }

        public async Task<DispatchValue> GetLastValue(Guid nodeInstanceId, CancellationToken token = default)
        {
            var dirName = ServerInfo.GetRemanentDirectory();
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
            try
            {
                var dirName = ServerInfo.GetRemanentDirectory();
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                var valuePath = Path.Combine(dirName, $"{nodeInstanceId}");
                // unique temp file (concurrent saves of the same node), replaced atomically - a crash never leaves a deleted or half written value
                var tempPath = Path.Combine(dirName, $"{nodeInstanceId}.{Guid.NewGuid():N}{TempFileExtension}");

                var jsonValue = JsonConvert.SerializeObject(value);

                try
                {
                    await using (var fileWriter = new StreamWriter(tempPath))
                    {
                        await fileWriter.WriteLineAsync(jsonValue);
                    }

                    File.Move(tempPath, valuePath, true);
                }
                finally
                {
                    TryDelete(tempPath);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
