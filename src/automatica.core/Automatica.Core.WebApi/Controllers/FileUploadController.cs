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
        public FileUploadController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpPost]
        [Authorize(Policy = Role.AdminRole)]
        public IActionResult Post()
        {
            // full path to file in temp location
            var myFile = Request.Form.Files[0];
            var targetLocation = ServerInfo.GetTempPath();

            try
            {
                var path = UploadPathHelper.GetSafeFilePath(targetLocation, myFile.FileName);

                // Uncomment to save the file
                using (var fileStream = System.IO.File.Create(path))
                {
                    myFile.CopyTo(fileStream);
                }
            }
            catch
            {
                Response.StatusCode = 400;
            }

            return new EmptyResult();
        }
    }
}
