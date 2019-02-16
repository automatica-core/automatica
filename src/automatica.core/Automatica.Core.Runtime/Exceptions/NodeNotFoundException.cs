using Automatica.Core.Base.Exceptions;
using System;

namespace Automatica.Core.Runtime.Exceptions
{
    [Serializable]
    public class NodeNotFoundException : WebApiException
    {
        public NodeNotFoundException() : 
            base("NODE_NOT_FOUND", ExceptionSeverity.Warning)
        {

        }
    }
}
