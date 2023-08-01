using System.Collections.Generic;

namespace P3.Synology.Api.Client.Shared.Models
{
    public class Error
    {
        public int Code { get; set; }

        public IEnumerable<Errors> Errors { get; set; }
    }
}
