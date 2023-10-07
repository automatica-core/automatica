using Automatica.Core.Runtime.Recorder.Base;

namespace Automatica.Core.Runtime.Recorder.Abstraction
{
    internal interface IRecorderFactory
    {
        IDataRecorderWriter GetRecorder(DataRecorderType recorderType);
    }
}
