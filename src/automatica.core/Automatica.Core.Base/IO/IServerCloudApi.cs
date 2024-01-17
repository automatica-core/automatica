using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Automatica.Core.Base.IO
{
    public interface IServerCloudApi
    {
        /// <summary>
        /// Sends an email to the given addresses
        /// </summary>
        /// <param name="to">To</param>
        /// <param name="subject">Subject</param>
        /// <param name="message">Message</param>
        /// <returns></returns>
        Task<bool> SendEmail(IList<string> to, string subject, string message);


        /// <summary>
        /// Converts a given text to speech audio
        /// </summary>
        /// <param name="id">A unique id for the request</param>
        /// <param name="text">Text to speak</param>
        /// <param name="language">Source text language</param>
        /// <param name="voice">Voice for the audio</param>
        /// <returns></returns>
        Task<string> Synthesize(Guid id, string text, string language, string voice);

        /// <summary>
        /// Converts a given text to speech audio
        /// </summary>
        /// <param name="id">A unique id for the request</param>
        /// <param name="text">Text to speak</param>
        /// <param name="language">Source text language</param>
        /// <param name="voice">Voice for the audio</param>
        /// <returns></returns>
        Task<(string uri, TimeSpan audioDuration)> SynthesizeWithAudioDuration(Guid id, string text, string language, string voice);
    }
}
