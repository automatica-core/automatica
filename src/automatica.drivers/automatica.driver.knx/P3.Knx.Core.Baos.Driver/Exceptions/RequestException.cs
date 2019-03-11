using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Knx.Core.Baos.Driver.Exceptions
{
    public enum ErrorCodes
    {
        NoError = 0,
        InternalError = 1,
        NoElementFound = 2,
        BufferIsTooSmall = 3,
        ItemIsNotWriteable = 4,
        ServiceIsNotSupported = 5,
        BadServiceParamter = 6,
        BadId = 7,
        BadCommandValue = 8,
        BadLength = 9,
        MessageIconsistent = 10,
        ObjectServerBusy = 11
    }
    public class RequestException : Exception
    {
        public RequestException(ErrorCodes errorCode)
        {
            ErrorCode = errorCode;
        }

        public ErrorCodes ErrorCode { get; }
    }
}
