namespace P3.Driver.HomeKit.Bonjour.Abstraction.Resolving
{
    /// <summary>
    ///   Locally held information on a domain name. 
    /// </summary>
    /// <remarks>
    ///   The domain name system is distributed, only a portion of the database
    ///   is available on each local host.
    /// </remarks>
    public class Node
    {
        /// <summary>
        ///   The name of the node.
        /// </summary>
        /// <value>
        ///   An absolute (fully qualified) domain name.  For example, "emanon.org".
        /// </value>
        /// <remarks>
        ///   All <see cref="Resources"/> must have a <see cref="ResourceRecord.Name"/> that
        ///   matches this value.
        /// </remarks>
        public DomainName Name { get; set; } = DomainName.Root;

        /// <inheritdoc />
        public override string ToString()
        {
            return Name.ToString();
        }

        /// <summary>
        ///   The resource records associated with this node.
        /// </summary>
        /// <value>
        ///   Commonly called the RRSET (resource record set).
        /// </value>
        /// <remarks>
        ///   Duplicate resources are silently ignored.
        /// </remarks>
        public ConcurrentSet<ResourceRecord> Resources { get; set;  } = new ConcurrentSet<ResourceRecord>();

        /// <summary>
        ///   Indicates that the node's resources contains the complete information for
        ///   the node.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the <see cref="Resources"/> are authoritative; otherwise, <b>false</b>.
        /// </value>
        /// <remarks>
        ///   An Authoritative node is typically defined in a <see cref="Catalog.IncludeZone">zone</see>.
        /// </remarks>
        public bool Authoritative { get; set; }

    }
}