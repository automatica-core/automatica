using System;

namespace P3.Knx.Core.Ets
{
    public class EtsProjectParserException : Exception
    {
        public EtsProjectParserException(Exception innerException)
            : this("KNX.ETS.UNKNOWN_ERROR", innerException)
        {
        }
        protected EtsProjectParserException(string message)
            : this(message, null)
        {
        }
        protected EtsProjectParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    public class EtsProjectParserPasswordRequiredException : EtsProjectParserException
    {
        public EtsProjectParserPasswordRequiredException()
            : base("KNX.ETS.PASSWORD_REQUIRED")
        {
        }
    }
    public class EtsProjectParserEmptyProjectFileException : EtsProjectParserException
    {
        public EtsProjectParserEmptyProjectFileException()
            : base("KNX.ETS.EMPTY_PROJECT")
        {
        }
    }
    public class EtsProjectParserTooManyProjectsInFileException : EtsProjectParserException
    {
        public EtsProjectParserTooManyProjectsInFileException()
            : base("KNX.ETS.TO_MANY_PROJECTS")
        {
        }
    }
    public class EtsProjectParserInvalidZipFileException : EtsProjectParserException
    {
        public EtsProjectParserInvalidZipFileException(Exception innerException)
            : base("KNX.ETS.ZIPFILE_ERROR", innerException)
        {
        }
    }
    public class EtsProjectParserWrongGroupAddressStyleException : EtsProjectParserException
    {
        public GroupAddressStyle Expected { get; }
        public GroupAddressStyle Actual { get; }

        public EtsProjectParserWrongGroupAddressStyleException(GroupAddressStyle expected, GroupAddressStyle actual)
            : base("KNX.ETS.WRONG_GROUP_STYLE")
        {
            Expected = expected;
            Actual = actual;
        }
    }
}
