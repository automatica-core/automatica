using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.UnitTests.Base.Common
{
    public class CloudApiMock : IServerCloudApi
    {
        public Task<bool> SendEmail(IList<string> to, string subject, string message)
        {
            return Task.FromResult(true);
        }

        public Task<string> Synthesize(Guid id, string text, string language, string voice)
        {
            return Task.FromResult(String.Empty);
        }

        public Task<(string uri, TimeSpan audioDuration)> SynthesizeWithAudioDuration(Guid id, string text, string language, string voice)
        {
            return Task.FromResult((String.Empty, TimeSpan.Zero));
        }
    }
}
