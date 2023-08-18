namespace Automatica.Core.Runtime.Recorder.Base
{
    public enum DataRecorderType
    {
        DatabaseRecorder = 0,
        CloudRecorder = 1,
        FileRecorder = 2,
        GraphiteRecorder = 3,
        HostedGrafanaRecorder = 4,
        MemoryRecorder = 5,
        HyperSeriesRecorder = 6
    }
}
