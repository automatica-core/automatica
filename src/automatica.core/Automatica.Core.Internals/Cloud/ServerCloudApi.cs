using Automatica.Core.Base.IO;
using Automatica.Core.Internals.Mail;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.Internals.Cloud
{
    internal class ServerCloudApi : IServerCloudApi
    {
        private readonly IMailService _mailService;
        private readonly TextToSpeechApi _textToSpeedApi;

        public ServerCloudApi(IMailService mailService, TextToSpeechApi textToSpeedApi)
        {
            _mailService = mailService;
            _textToSpeedApi = textToSpeedApi;
        }

        /// <summary>
        /// Through the own SMTP server when it is switched on in the settings, otherwise through the cloud
        /// </summary>
        public async Task<bool> SendEmail(IList<string> to, string subject, string message)
        {
            return (await _mailService.SendAsync(to, subject, message).ConfigureAwait(false)).Success;
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
