using System;

namespace Automatica.Driver.Shelly
{
    public class ShellyResult<T>
    {
        public T Value
        {
            get
            {
                if (!_successChecked)
                {
                    throw new InvalidOperationException("Cannot access value of result without checking success first");
                }

                return _value;
            }
        }

        /// <summary>
        /// Indicates the client request has completed successfully
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                _successChecked = true;
                return _success;
            }
        }
        
        /// <summary>
        /// Indicates the client request has failed
        /// </summary>
        public bool IsFailure => !IsSuccess;
        
        /// <summary>
        /// Indicates if the reason for failure is transient 
        /// </summary>
        public bool IsTransient { get; private set; }
        
        /// <summary>
        /// Any message that accompanies this result
        /// </summary>
        public string Message { get; }

        private T _value;
        private bool _successChecked = false;
        private bool _success = false;
        
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