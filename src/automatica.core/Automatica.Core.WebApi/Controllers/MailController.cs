using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Mail;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    public sealed class MailTestDto
    {
        /// <summary>
        /// Recipient, empty = the sender address of the SMTP settings
        /// </summary>
        public string To { get; set; }
    }

    /// <summary>
    /// The own SMTP server of the settings (group SERVER.SMTP). Admins only, like the settings.
    /// </summary>
    [Route("webapi/mail")]
    [Authorize(Policy = Role.AdminRole)]
    public class MailController : BaseController
    {
        private readonly IMailService _mailService;

        public MailController(AutomaticaContext dbContext, IMailService mailService) : base(dbContext)
        {
            _mailService = mailService;
        }

        /// <summary>
        /// Sends a test mail with the saved SMTP settings (also when the SMTP server is not switched on yet)
        /// </summary>
        [HttpPost("test")]
        public Task<MailResult> SendTest([FromBody] MailTestDto dto, CancellationToken token)
        {
            return _mailService.SendSmtpTestAsync(dto?.To, token);
        }
    }
}
