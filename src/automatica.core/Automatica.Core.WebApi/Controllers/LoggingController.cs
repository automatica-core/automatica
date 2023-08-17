using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Automatica.Core.WebApi.Controllers
{
    [Route("webapi/logging")]
    public class LoggingController : BaseController
    {
        private readonly IFileProvider _fileProvider;

        public LoggingController(AutomaticaContext dbContext) : base(dbContext)
        {
            _fileProvider = new PhysicalFileProvider(ServerInfo.GetBasePath());
        }

        [HttpGet]
        [Route("files")]

        [Authorize(Policy = Role.AdminRole)]
        public List<string> GetLogFiles()
        {
            var files = GetFilesForSubDirectory(Path.Combine(ServerInfo.GetBasePath(), "logs"));

            return files.Select(a => a.FullName.Replace(ServerInfo.GetBasePath(), "").Replace("\\", "/")).ToList();
        }


        [HttpGet]
        [Route("file")]

        [Authorize(Policy = Role.AdminRole)]
        public async Task GetLogFile([FromQuery] string file)
        {
            var files = GetFilesForSubDirectory(Path.Combine(ServerInfo.GetBasePath(), "logs"));

            var logList = files.Select(a => a.FullName.Replace(ServerInfo.GetBasePath(), "").Replace("\\", "/")).ToList();

            if (!logList.Contains(file))
            {
                throw new ArgumentException($"{file} is not a logfile!");
            }

            var fileInfo = _fileProvider.GetFileInfo(Path.Combine(ServerInfo.GetBasePath(), file));

            await HttpContext.Response.SendFileAsync(fileInfo);


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