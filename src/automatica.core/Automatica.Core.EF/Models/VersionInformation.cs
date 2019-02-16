using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.EF.Models
{
    public partial class VersionInformation
    {
        public long ObjId { get; set; }
        public Guid? DriverGuid { get; set; }
        public Guid RuleGuid { get; set; }
        public string Version { get; set; }

        public string Name { get; set; }

        [NotMapped]
        public Version VersionData => new Version(Version);

    }
}
