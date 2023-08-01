using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.IO
{
    public class ImportConfig
    {
        public NodeInstance Node { get; set; }
        public string FileName { get; set; }
        public string Password { get; set; }
    }
}
