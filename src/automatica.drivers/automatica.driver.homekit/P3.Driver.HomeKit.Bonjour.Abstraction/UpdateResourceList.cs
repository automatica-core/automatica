using System;
using System.Collections.Generic;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Resource records to add or delete from the zone.
    /// </summary>
    /// <remarks>
    ///   The list of <see cref="ResourceRecord">resource records</see> which are
    ///   adde or deleted from the <see cref="UpdateMessage.Zone"/>.
    ///   <para>
    ///   <c>AddResource</c> and <c>DeleteResource</c> are convenience methods to specify
    ///   the update operations.
    ///   </para>
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc2136"/>
    public class UpdateResourceList : List<ResourceRecord>
    {
        /// <summary>
        ///   Add the resource to the zone.
        /// </summary>
        /// <param name="resource">
        ///   The <see cref="ResourceRecord"/> to add to the zone.
        /// </param>
        /// <returns>
        ///   The update resource list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   Equivalent to <see cref="List{T}.Add"/>.
        ///   <para>
        ///   A duplicate <see cref="ResourceRecord"/> will be silently ignored by the primary
        ///   master.
        ///   </para>
        /// </remarks>
        public UpdateResourceList AddResource(ResourceRecord resource)
        {
            this.Add(resource);
            return this;
        }

        /// <summary>
        ///   Delete the resource record from the zone.
        /// </summary>
        /// <param name="resource">
        ///   The <see cref="ResourceRecord"/> to delete from the zone.
        /// </param>
        /// <returns>
        ///   The update resource list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   The NAME, TYPE, RDLENGTH and RDATA must match the RR being deleted.
        ///   TTL must be specified as zero(0) and will otherwise be ignored by the primary
        ///   master. CLASS must be specified as NONE to distinguish this from an
        ///   RR addition.
        ///   <para>
        ///   If no such RRsets exist, then
        ///   this Update RR will be silently ignored by the primary master.
        ///   </para>
        /// </remarks>
        public UpdateResourceList DeleteResource(ResourceRecord resource)
        {
            resource.Class = DnsClass.None;
            resource.TTL = TimeSpan.Zero;
            this.Add(resource);
            return this;
        }

        /// <summary>
        ///   Delete the resource records with the specifified name.
        /// </summary>
        /// <param name="name">A resource name.</param>
        /// <returns>
        ///   The update resource list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   TYPE must be specified as ANY.  TTL must
        ///   be specified as zero(0) and is otherwise not used by the primary
        ///   master. CLASS must be specified as ANY. RDLENGTH must be zero(0)
        ///   and RDATA must therefore be empty.
        ///   <para>
        ///   If no such RRsets exist, then
        ///   this Update RR will be silently ignored by the primary master.
        ///   </para>
        /// </remarks>
        public UpdateResourceList DeleteResource(DomainName name)
        {
            var resource = new ResourceRecord
            {
                Name = name,
                Class = DnsClass.ANY,
                Type = DnsType.ANY,
                TTL = TimeSpan.Zero
            };
            this.Add(resource);
            return this;
        }

        /// <summary>
        ///   Delete the resource records with the specifified name and type.
        /// </summary>
        /// <param name="name">A resource name.</param>
        /// <param name="type">One of the RR TYPE codes.</param>
        /// <returns>
        ///   The update resource list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   TTL must be specified as zero(0) and is otherwise not used by the primary
        ///   master. CLASS must be specified as ANY. RDLENGTH must be zero(0)
        ///   and RDATA must therefore be empty.
        ///   <para>
        ///   If no such RRsets exist, then
        ///   this Update RR will be silently ignored by the primary master.
        ///   </para>
        /// </remarks>
        /// <seealso cref="DeleteResource{T}(DomainName)"/>
        public UpdateResourceList DeleteResource(DomainName name, DnsType type)
        {
            var resource = new ResourceRecord
            {
                Name = name,
                Class = DnsClass.ANY,
                Type = type,
                TTL = TimeSpan.Zero
            };
            this.Add(resource);
            return this;
        }

        /// <summary>
        ///   Delete the resource records with the specifified name and type.
        /// </summary>
        /// <param name="name">A resource name.</param>
        /// <typeparam name="T">
        ///   A derived class of <see cref="ResourceRecord"/>.
        /// </typeparam>
        /// <returns>
        ///   The update resource list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   TTL must be specified as zero(0) and is otherwise not used by the primary
        ///   master. CLASS must be specified as ANY. RDLENGTH must be zero(0)
        ///   and RDATA must therefore be empty.
        ///   <para>
        ///   If no such RRsets exist, then
        ///   this Update RR will be silently ignored by the primary master.
        ///   </para>
        /// </remarks>
        /// <seealso cref="DeleteResource(DomainName, DnsType)"/>
        public UpdateResourceList DeleteResource<T>(DomainName name)
             where T : ResourceRecord, new()
        {
            return DeleteResource(name, new T().Type);
        }

    }
}
