using System;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Runtime.IO
{
    public class DispatchLoopDetectedException : Exception
    {
        public IDispatchable Dispatchable { get; }

        public DispatchLoopDetectedException(IDispatchable dispatchable)
        {
            Dispatchable = dispatchable;
        }
    }
}
