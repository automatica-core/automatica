using Automatica.Driver.Shelly.Common;
using System;

namespace Automatica.Driver.Shelly
{
    public class ShellyResult<T> : IShellyResult<T>
    {
        public T Value => _value;

        /// <summary>
        /// Indicates the client request has completed successfully
        /// </summary>
        public bool IsSuccess => _success;
        
        /// <summary>
        /// Indicates if the reason for failure is transient 
        /// </summary>
        public bool IsTransient { get; private set; }
        
        /// <summary>
        /// Any message that accompanies this result
        /// </summary>
        public string Message { get; }

        private readonly T _value;
        private readonly bool _success = false;
        
        private ShellyResult(T value, bool success, bool isTransient, string message = null)
        {
            _value = value;
            _success = success;
            IsTransient = isTransient;
            Message = message;
        }

        public static ShellyResult<T> Success(T value, string message = null)
        {
            return new ShellyResult<T>(value, true, false,message);
        }

        public static ShellyResult<T> Failure(string message = null)
        {
            return new ShellyResult<T>(default, success: false, isTransient: false, message);
        }

        public static ShellyResult<T> TransientFailure(string message = null)
        {
            return new ShellyResult<T>(default, success: false, isTransient: true, message);
        }
    }
}