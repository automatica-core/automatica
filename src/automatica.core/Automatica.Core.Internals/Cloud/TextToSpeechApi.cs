using System;
using System.Threading.Tasks;
using Automatica.Core.Internals.Cloud.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Cloud
{
    internal class TextToSpeechApi : BaseCloudApi, ITextToSpeechApi
    {
        private readonly ILogger<CloudApi> _logger;

        public TextToSpeechApi(IConfiguration config, ILogger<CloudApi> logger) : base(config, logger)
        {
            _logger = logger;
        }

        public async Task<string> Synthesize(Guid id, string text, string language, string voice)
        {

            try
            {
                var response = await PostRequest<TextToSpeechResponse>($"/{WebApiPrefix}/{WebApiVersion}/tts",
                    new TextToSpeechRequest { Id = id, Language = language, Text = text, Voice = voice });
                return response.Url;
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not synthesize text ({text}, {language}, {voice}) {e}");
            }
            throw new NotImplementedException();
        }

    }
}
