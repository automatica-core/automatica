namespace P3.Driver.VkingBms.Driver.Exception
{
    public class DataReadException : System.Exception
    {
        public DataReadException()
        {
        }

        public DataReadException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// The checksum of a received frame does not match, only the current frame is invalid, the port is still usable
    /// </summary>
    public class InvalidCrcException : DataReadException
    {
        public InvalidCrcException(string message) : base(message)
        {
        }
    }
}
