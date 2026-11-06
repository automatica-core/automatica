using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Automatica.Core.Base.Common;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/FileUpload")]
    public class FileUploadController: BaseController
    {
        /// <summary>
        /// Upload size limit (ets projects,...)
        /// </summary>
        public const long MaxUploadSize = 256L * 1024 * 1024;

        public FileUploadController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        /// <summary>
        /// Stores the file with a random name in the private upload directory, the client name is never used as a path.
        /// </summary>
        /// <returns>{ fileName }: the name to pass to the import</returns>
        [HttpPost]
        [Authorize(Policy = Role.AdminRole)]
        [RequestSizeLimit(MaxUploadSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSize)]
        public IActionResult Post()
        {
            if (Request.Form.Files.Count == 0)
            {
                return BadRequest();
            }

            var myFile = Request.Form.Files[0];
            if (myFile.Length > MaxUploadSize)
            {
                return BadRequest("File too large");
            }

            try
            {
                var fileName = UploadPathHelper.CreateRandomFileName(myFile.FileName);
                var path = UploadPathHelper.GetSafeFilePath(ServerInfo.GetUploadDirectory(), fileName);

                using (var fileStream = new FileStream(path, FileMode.CreateNew))
                {
                    myFile.CopyTo(fileStream);
                }
                ServerInfo.MakeFilePrivate(path);

                return Ok(new { fileName });
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
