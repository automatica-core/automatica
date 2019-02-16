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
    }
}
