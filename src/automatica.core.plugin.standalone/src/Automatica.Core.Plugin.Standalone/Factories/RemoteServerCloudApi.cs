using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    internal class RemoteServerCloudApi : IServerCloudApi
    {
        public Task<bool> SendEmail(IList<string> to, string subject, string message)
        {
            return Task.FromResult(false);
        }

        public Task<string> Synthesize(Guid id, string text, string language, string voice)
        {
            return Task.FromResult(""); }

        public Task<(string uri, TimeSpan audioDuration)> SynthesizeWithAudioDuration(Guid id, string text, string language, string voice)
        {
            return Task.FromResult(("", TimeSpan.Zero));
        }
    }
}
