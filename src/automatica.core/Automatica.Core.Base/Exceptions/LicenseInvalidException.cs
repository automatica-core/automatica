namespace Automatica.Core.Base.Exceptions
{
    public class LicenseInvalidException : WebApiException
    {
        public LicenseInvalidException(string licenseKey) : base($"{licenseKey}", ExceptionSeverity.Error)
        {
        }
    }
}
