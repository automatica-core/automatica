using System;

namespace Automatica.Core.Base.IO
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
