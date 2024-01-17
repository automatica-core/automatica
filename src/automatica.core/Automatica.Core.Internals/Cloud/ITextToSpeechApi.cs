using Automatica.Core.Internals.Cloud.Model;
using System;
using System.Threading.Tasks;

namespace Automatica.Core.Internals.Cloud
{
    public interface ITextToSpeechApi
    {
        /// <summary>
        /// Converts a given text to speech audio
        /// </summary>
        /// <param name="id">A unique id for the request</param>
        /// <param name="text">Text to speak</param>
        /// <param name="language">Source text language</param>
        /// <param name="voice">Voice for the audio</param>
        /// <returns></returns>
        Task<TextToSpeechResponse> Synthesize(Guid id, string text, string language, string voice);
    }
}
