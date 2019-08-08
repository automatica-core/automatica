using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Recorder
{
    internal interface IDataRecorder
    {
        void ValueChanged(object value, string name);
        Task Stop();
        Task Start();
    }
}
