using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Habits
{
    public enum HabitActionOrigin
    {
        /// <summary>Written by a user in a visualization (DataHub)</summary>
        Visualization = 0,
        /// <summary>A driver reported a new value of a data point (a pressed button, a sensor)</summary>
        DataPoint = 1,
        /// <summary>A measurement (power, temperature, ...): the time-weighted average of a 15 min slot, with its min and max</summary>
        Measurement = 2
    }

    /// <summary>
    /// One action (a visu tap or a value change of a data point), recorded for habit suggestions while the setting
    /// <c>habitsEnabled</c> is on. Sun position is not stored: it follows from <see cref="UtcTime"/> and the
    /// installation's latitude/longitude.
    /// </summary>
    public class HabitAction : TypedObject
    {
        public Guid ObjId { get; set; }

        public DateTime UtcTime { get; set; }
        /// <summary>Server local time, for the time-of-day and weekday of the action</summary>
        public DateTime LocalTime { get; set; }

        /// <summary>Node instance or logic interface that was written</summary>
        public Guid TargetId { get; set; }
        public Guid? This2AreaInstance { get; set; }
        public Guid? This2CategoryInstance { get; set; }

        public string Value { get; set; }
        /// <summary>Lowest and highest value of a <see cref="HabitActionOrigin.Measurement"/> slot, empty for actions</summary>
        public double? Min { get; set; }
        public double? Max { get; set; }
        public HabitActionOrigin Origin { get; set; }

        /// <summary>User who tapped in the visu, empty for data point changes</summary>
        public Guid? UserId { get; set; }

        /// <summary>Active operating modes, comma separated</summary>
        public string OperatingModes { get; set; }
    }
}
