using System;

namespace P3.Driver.Sonos.Upnp.Proxy
{
    public class UpnpArgument
    {
        public string Name { get; }

        public object Value { get; }

        public UpnpArgument(string name, object value)
        {
            if(string.IsNullOrEmpty(name))
                throw new ArgumentException("Name was null or empty.", nameof(name));

            Name = name;
            Value = value;
        }

        public static UpnpArgument CreateInstanceId()
        {
            return new UpnpArgument("InstanceID", 0);
        }
    }
}
