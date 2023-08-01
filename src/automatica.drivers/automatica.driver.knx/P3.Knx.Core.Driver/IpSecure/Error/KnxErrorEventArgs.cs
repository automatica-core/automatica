using System;

namespace P3.Knx.Core.Driver.IpSecure.Error
{
    public class KnxErrorEventArgs : EventArgs
    {
        public KnxErrorEventArgs(string text, KnxError error)
        {
            ErrorType = error;
            Text = text;
        }

        public string Text { get; set; }
        public KnxError ErrorType { get; }

        public override string ToString()
        {
            return $"{Text} - {ErrorType}";
        }
    }
}
