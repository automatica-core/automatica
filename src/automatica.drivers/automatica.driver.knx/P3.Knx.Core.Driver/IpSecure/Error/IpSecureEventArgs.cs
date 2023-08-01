namespace P3.Knx.Core.Driver.IpSecure.Error
{
    public class IpSecureEventArgs : KnxErrorEventArgs
    {
        public IpSecureEventArgs(string text, KnxError error)
            : base(text, error)
        {
        }
    }
}
