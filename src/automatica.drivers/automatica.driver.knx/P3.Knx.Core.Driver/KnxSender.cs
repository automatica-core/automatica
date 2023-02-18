using P3.Knx.Core.Driver.Frames;

namespace P3.Knx.Core.Driver
{
    internal abstract class KnxSender
    {
        protected KnxSender(KnxConnection connection)
        {
            Connection = connection;
        }

        public KnxConnection Connection { get; }

        internal abstract bool SendFrame(KnxFrame frame);

        internal abstract void Start();
        internal abstract void Stop();

    }
}
