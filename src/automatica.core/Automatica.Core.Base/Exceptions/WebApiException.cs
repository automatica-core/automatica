using Newtonsoft.Json;
using System;
using System.Dynamic;

namespace Automatica.Core.Base.Exceptions
{

    public enum ExceptionSeverity
    {
        Info,
        Warning,
        Error,
        Dead
    }

    /// <summary>
    /// Provides a serializeable exception for the UI
    /// </summary>
    [Serializable]
    public class WebApiException : Exception
    {
        public WebApiException(string errorText, ExceptionSeverity severity)
        {
            ErrorText = errorText;
            Severity = severity;
        }
        public string ErrorText { get; }
        public ExceptionSeverity Severity { get; }


        public string ToJson()
        {
            dynamic obj = new ExpandoObject();
            obj.errorText = ErrorText;
            obj.severity = Severity;

            obj.TypeInfo = "WebApiException";
            return JsonConvert.SerializeObject(obj);
        }
    }
}
