using System;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.LightingController
{
    /// <summary>
    /// The interface template ids of a lighting controller. Template ids are global, so the hotel lighting controller has its own set.
    /// </summary>
    public sealed class LightingControllerIds
    {
        public const int Circuits = 8;

        public Guid MoodPlus { get; init; }
        public Guid MoodMinus { get; init; }
        public Guid On { get; init; }
        public Guid Off { get; init; }
        public Guid MoodSelect { get; init; }
        public Guid Motion { get; init; }
        public Guid Brightness { get; init; }
        public Guid DisableMotion { get; init; }
        public Guid Command { get; init; }

        public Guid ActiveMood { get; init; }
        public Guid AnyOn { get; init; }
        public Guid VisuState { get; init; }

        public Guid Moods { get; init; }
        public Guid MotionOffDelay { get; init; }
        public Guid BrightnessThreshold { get; init; }
        public Guid MotionMood { get; init; }
        public Guid ManualOffDelay { get; init; }
        public Guid AutomaticTimeout { get; init; }

        public Guid[] CircuitOutputs { get; init; }
        public Guid[] KelvinOutputs { get; init; }
        public Guid[] CircuitTypes { get; init; }
        public Guid[] CircuitNames { get; init; }

        public PresenceSimulationIds PresenceSimulation { get; init; }

        /// <summary>
        /// Ids derived from a fixed prefix - never change the prefix of a released block
        /// </summary>
        public static LightingControllerIds Derive(string prefix)
        {
            Guid Of(string name) => TemplateIds.Of($"{prefix}.{name}");
            return new LightingControllerIds
            {
                MoodPlus = Of("MoodPlus"),
                MoodMinus = Of("MoodMinus"),
                On = Of("On"),
                Off = Of("Off"),
                MoodSelect = Of("MoodSelect"),
                Motion = Of("Motion"),
                Brightness = Of("Brightness"),
                DisableMotion = Of("DisableMotion"),
                Command = Of("Command"),
                ActiveMood = Of("ActiveMood"),
                AnyOn = Of("AnyOn"),
                VisuState = Of("VisuState"),
                Moods = Of("Moods"),
                MotionOffDelay = Of("MotionOffDelay"),
                BrightnessThreshold = Of("BrightnessThreshold"),
                MotionMood = Of("MotionMood"),
                ManualOffDelay = Of("ManualOffDelay"),
                AutomaticTimeout = Of("AutomaticTimeout"),
                CircuitOutputs = TemplateIds.Series($"{prefix}.CircuitOutput", Circuits),
                KelvinOutputs = TemplateIds.Series($"{prefix}.KelvinOutput", Circuits),
                CircuitTypes = TemplateIds.Series($"{prefix}.CircuitType", Circuits),
                CircuitNames = TemplateIds.Series($"{prefix}.CircuitName", Circuits),
                PresenceSimulation = PresenceSimulationIds.Derive(prefix)
            };
        }
    }
}
