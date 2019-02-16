using System;
using System.Collections.Generic;
using System.Text;

namespace Automatica.Core.Base.Exceptions
{
    public class LicenseInvalidException : WebApiException
    {
        public LicenseInvalidException(string licenseKey) : base($"{licenseKey}", ExceptionSeverity.Error)
        {
        }
    }
}
