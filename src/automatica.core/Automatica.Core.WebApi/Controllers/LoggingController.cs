using System.Collections.Generic;
using System.IO;
using System.Linq;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/logging")]
    public class LoggingController : BaseController
    {
        public LoggingController(AutomaticaContext dbContext) : base(dbContext)
        {
        }

        [HttpGet]
        [Route("files")]

        [Authorize(Policy = Role.AdminRole)]
        public List<string> GetLogFiles()
        {
            var files = GetFilesForSubDirectory(Path.Combine(ServerInfo.GetBasePath(), "logs"));
            
            return files.Select(a => a.FullName.Replace(ServerInfo.GetBasePath(), "")).ToList();
        }

        private List<FileInfo> GetFilesForSubDirectory(string directory)
        {
            var ret = new List<FileInfo>();
            var directories = Directory.GetDirectories(directory);
            var files = Directory.GetFiles(directory);

            foreach (var dir in directories)
            {
                ret.AddRange(GetFilesForSubDirectory(dir));
            }

            foreach (var file in files)
            {
                ret.Add(new FileInfo(file));
            }

            return ret;
        }
    }
}