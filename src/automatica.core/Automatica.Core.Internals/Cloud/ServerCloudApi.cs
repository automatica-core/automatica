using Automatica.Core.Base.IO;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.Internals.Cloud
{
    internal class ServerCloudApi : IServerCloudApi
    {
        private readonly CloudApi _cloudApi;
        private readonly TextToSpeechApi _textToSpeedApi;

        public ServerCloudApi(CloudApi cloudApi, TextToSpeechApi textToSpeedApi)
        {
            _cloudApi = cloudApi;
            _textToSpeedApi = textToSpeedApi;
        }

        public Task<bool> SendEmail(IList<string> to, string subject, string message)
        {
            return _cloudApi.SendEmail(to, subject, message);
        }

        public async Task<string> Synthesize(Guid id, string text, string language, string voice)
        {
            return (await _textToSpeedApi.Synthesize(id, text, language, voice)).Url;
        }

        public async Task<(string uri, TimeSpan audioDuration)> SynthesizeWithAudioDuration(Guid id, string text, string language, string voice)
        {
            var response = await _textToSpeedApi.Synthesize(id, text, language, voice);

            return (response.Url, response.AudioDuration);
        }
    }
}
