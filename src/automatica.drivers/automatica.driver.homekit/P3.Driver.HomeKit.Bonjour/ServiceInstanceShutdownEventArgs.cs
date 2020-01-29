using P3.Driver.HomeKit.Bonjour.Abstraction;

namespace P3.Driver.HomeKit.Bonjour
{
    /// <summary>
    ///   The event data for <see cref="ServiceDiscovery.ServiceInstanceShutdown"/>.
    /// </summary>
    public class ServiceInstanceShutdownEventArgs : MessageEventArgs
    {
        /// <summary>
        ///   The fully qualified name of the service instance.
        /// </summary>
        /// <value>
        ///   Typically of the form "<i>instance</i>._<i>service</i>._tcp.local".
        /// </value>
        /// <seealso cref="ServiceProfile.FullyQualifiedName"/>
        public DomainName ServiceInstanceName { get; set; }
    }
}

