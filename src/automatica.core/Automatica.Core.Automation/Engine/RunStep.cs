namespace Automatica.Core.Automation.Engine
{
    public enum RunStepKind
    {
        Trigger,
        Condition,
        Action,
        Info,
        Error
    }

    public enum RunStepState
    {
        Running,
        Waiting,
        Done,
        Failed,
        Skipped,
        Timeout
    }

    /// <summary>
    /// One entry in the history of a run. <see cref="Path"/> points into the definition (see README "Paths").
    /// </summary>
    public sealed class RunStep
    {
        /// <summary>UTC</summary>
        public DateTime At { get; set; }
        public RunStepKind Kind { get; set; }
        public string Path { get; set; }
        public bool? Passed { get; set; }
        public object Value { get; set; }
        public object Previous { get; set; }
        public long? DurationMs { get; set; }
        public RunStepState? State { get; set; }
        public string Message { get; set; }

        public RunStep Clone()
        {
            return (RunStep)MemberwiseClone();
        }
    }
}
