using System;

namespace Automatica.Core.Driver.Exceptions
{
    /// <summary>
    /// Invalid input value exception
    /// </summary>
    public class InvalidInputValueException : Exception
    {

        public InvalidInputValueException()
        {
            
        }

        public InvalidInputValueException(Exception innerException) : base("InnerException", innerException)
        {
        }
    }
}
