namespace Automatica.Core.Runtime.Recorder
{
    public interface IRecorderFactory
    {
        IDataRecorderWriter GetRecorder(DataRecorderType recorderType);
    }
}
