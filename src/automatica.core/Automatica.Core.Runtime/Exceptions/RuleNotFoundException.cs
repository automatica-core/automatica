using Automatica.Core.Base.Exceptions;
using System;

namespace Automatica.Core.Runtime.Exceptions
{
    [Serializable]
    public class RuleNotFoundException : WebApiException
    {
        public RuleNotFoundException() : 
            base("RULE_NOT_FOUND", ExceptionSeverity.Warning)
        {

        }
    }
}
