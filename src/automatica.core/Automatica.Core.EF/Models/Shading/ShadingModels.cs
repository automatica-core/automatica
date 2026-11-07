using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Shading
{
    /// <summary>
    /// Global settings of the automatic shading, a single row (<see cref="SingletonId"/>). The settings are a JSON
    /// document (see Automatica.Core.Shading.Definition.ShadingSettingsDefinition).
    /// </summary>
    public class ShadingSettings : TypedObject
    {
        public static readonly Guid SingletonId = new Guid("6f0c9a3e-2b1d-4c8e-9a47-5d3e1f2b7c10");

        public Guid ObjId { get; set; }
        public string Definition { get; set; }
        public DateTime ModifiedAt { get; set; }
    }

    /// <summary>
    /// A side of the building: the direction its windows face (azimuth in degrees, 0 north, 90 east, 180 south,
    /// 270 west) and its brightness sensor (JSON document, see Automatica.Core.Shading.Definition.FacadeDefinition).
    /// </summary>
    public class ShadingFacade : TypedObject
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public double Azimuth { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Definition { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }

    /// <summary>
    /// The shading settings of a blind control (a shutter logic instance), JSON document see
    /// Automatica.Core.Shading.Definition.BlindDefinition. Deleted with the logic.
    /// </summary>
    public class ShadingBlind : TypedObject
    {
        public Guid ObjId { get; set; }

        public Guid This2RuleInstance { get; set; }
        public RuleInstance This2RuleInstanceNavigation { get; set; }

        public Guid? This2ShadingFacade { get; set; }
        public ShadingFacade This2ShadingFacadeNavigation { get; set; }

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// The automatic of this blind is paused (by the visualization or an automation), it survives a restart
        /// </summary>
        public bool IsPaused { get; set; }

        public string Definition { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
