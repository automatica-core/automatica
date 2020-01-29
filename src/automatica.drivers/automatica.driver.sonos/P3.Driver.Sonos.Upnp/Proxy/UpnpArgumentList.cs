using System.Collections.Generic;
using System.Linq;

namespace P3.Driver.Sonos.Upnp.Proxy
{
    public class UpnpArgumentList
    {
        public IList<UpnpArgument> Arguments { get; }

        public UpnpArgumentList() : this(null)
        {
        }

        public UpnpArgumentList(IList<UpnpArgument> args)
        {
            if (args == null)
                args = new List<UpnpArgument>();

            AddInstanceIdArg(args);

            Arguments = args;
        }

        private static void AddInstanceIdArg(IList<UpnpArgument> args)
        {
            var instanceId = UpnpArgument.CreateInstanceId();

            if (args.All(p => p.Name != instanceId.Name))
                args.Insert(0, instanceId);
        }
    }
}