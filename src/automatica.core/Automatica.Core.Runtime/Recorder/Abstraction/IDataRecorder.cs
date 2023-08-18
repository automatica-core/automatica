using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Runtime.Recorder.Abstraction
{
    internal interface IDataRecorder
    {
        void ValueChanged(DispatchValue value, string name);
        Task Stop();
        Task Start();
    }
}
