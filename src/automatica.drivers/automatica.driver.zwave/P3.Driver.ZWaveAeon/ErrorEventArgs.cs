using System;

namespace P3.Driver.ZWaveAeon
{
    public class ErrorEventArgs : EventArgs
    {
        public readonly Exception Error;

        public ErrorEventArgs(Exception error)
        {
            Error = error;
        }
    }
}
