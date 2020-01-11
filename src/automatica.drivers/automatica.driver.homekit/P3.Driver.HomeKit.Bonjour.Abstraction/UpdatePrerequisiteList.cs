using System;
using System.Collections.Generic;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Preconditions for a update.
    /// </summary>
    /// <remarks>
    ///   The list of <see cref="ResourceRecord">resource records</see> which must be
    ///   satisfied before an <see cref="UpdateMessage"/> can proceed.
    ///   <para>
    ///   <c>MustExist</c> and <c>MustNotExist</c> are convenience methods to create the 
    ///   various preconditions.
    ///   </para>
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc2136"/>
    public class UpdatePrerequisiteList : List<ResourceRecord>
    {
        /// <summary>
        ///   At least one resource record with the specified name and type must exist
        ///   in the <see cref="UpdateMessage.Zone"/>.
        /// </summary>
        /// <param name="name">A resource name.</param>
        /// <param name="type">One of the RR TYPE codes.</param>
        /// <returns>
        ///   The prerequisite list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   For this prerequisite, a requestor adds to the section a single RR
        ///   whose NAME and TYPE are equal to that of the zone RRset whose
        ///   existence is required. RDLENGTH is zero and RDATA is therefore
        ///   empty. CLASS must be specified as ANY to differentiate this
        ///   condition from that of an actual RR whose RDLENGTH is naturally zero
        ///   (0) (e.g., NULL).  TTL is specified as zero(0).
        /// </remarks>
        public UpdatePrerequisiteList MustExist(DomainName name, DnsType type)
        {
            var rr = new ResourceRecord
            {
                Name = name,
                Type = type,
                Class = DnsClass.ANY,
                TTL = TimeSpan.Zero
            };
            this.Add(rr);
            return this;
        }

        /// <summary>
        ///   At least one resource record with the specified name must exist
        ///   in the <see cref="UpdateMessage.Zone"/>.
        /// </summary>
        /// <param name="name">A resource name.</param>
        /// <returns>
        ///   The prerequisite list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   For this prerequisite, a requestor adds to the section a single RR
        ///   whose NAME is equal to that of the name whose ownership of an RR is
        ///   required. RDLENGTH is zero and RDATA is therefore empty. CLASS must
        ///   be specified as ANY to differentiate this condition from that of an
        ///   actual RR whose RDLENGTH is naturally zero (0) (e.g., NULL).  TYPE
        ///   must be specified as ANY to differentiate this case from that of an
        ///   RRset existence test. TTL is specified as zero (0).
        /// </remarks>
        public UpdatePrerequisiteList MustExist(DomainName name)
        {
            return MustExist(name, DnsType.ANY);
        }

        /// <summary>
        ///   At least one resource record with the specified name and type must exist
        ///   in the <see cref="UpdateMessage.Zone"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   A derived class of <see cref="ResourceRecord"/>.
        /// </typeparam>
        /// <param name="name">A resource name.</param>
        /// <returns>
        ///   The prerequisite list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   For this prerequisite, a requestor adds to the section a single RR
        ///   whose NAME is equal to that of the name whose ownership of an RR is
        ///   required. RDLENGTH is zero and RDATA is therefore empty. CLASS must
        ///   be specified as ANY to differentiate this condition from that of an
        ///   actual RR whose RDLENGTH is naturally zero (0) (e.g., NULL).  TYPE
        ///   must be specified as ANY to differentiate this case from that of an
        ///   RRset existence test. TTL is specified as zero (0).
        /// </remarks>
        public UpdatePrerequisiteList MustExist<T>(DomainName name)
            where T : ResourceRecord, new()
        {
            return MustExist(name, new T().Type);
        }

        /// <summary>
        ///   A resource record exists with the specified NAME, TYPE and RDATA.
        /// </summary>
        /// <param name="resource">A resource record.</param>
        /// <returns>
        ///   The prerequisite list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   For this prerequisite, a requestor adds to the section an entire
        ///   RRset whose preexistence is required. NAME and TYPE are that of the
        ///   RRset being denoted. CLASS is that of the zone.  TTL must be
        ///   specified as zero (0) and is ignored when comparing RRsets for
        ///   identity.
        /// </remarks>
        public UpdatePrerequisiteList MustExist(ResourceRecord resource)
        {
            resource.TTL = TimeSpan.Zero;
            this.Add(resource);
            return this;
        }

        /// <summary>
        ///   No resource record with the specified name and type can exist
        ///   in the <see cref="UpdateMessage.Zone"/>.
        /// </summary>
        /// <param name="name">A resource name.</param>
        /// <param name="type">One of the RR TYPE codes.</param>
        /// <returns>
        ///   The prerequisite list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   For this prerequisite, a requestor adds to the section a single RR
        ///   whose NAME and TYPE are equal to that of the RRset whose nonexistence
        ///   is required. The RDLENGTH of this record is zero (0), and RDATA
        ///   field is therefore empty.  CLASS must be specified as NONE in order
        ///   to distinguish this condition from a valid RR whose RDLENGTH is
        ///   naturally zero (0) (for example, the NULL RR).  TTL must be specified
        ///   as zero(0).
        /// </remarks>
        public UpdatePrerequisiteList MustNotExist(DomainName name, DnsType type)
        {
            var rr = new ResourceRecord
            {
                Name = name,
                Type = type,
                Class = DnsClass.None,
                TTL = TimeSpan.Zero
            };
            this.Add(rr);
            return this;
        }

        /// <summary>
        ///   No resource record with the specified name can exist
        ///   in the <see cref="UpdateMessage.Zone"/>.
        /// </summary>
        /// <param name="name">A resource name.</param>
        /// <returns>
        ///   The prerequisite list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   For this prerequisite, a requestor adds to the section a single RR
        ///   whose NAME is equal to that of the name whose nonownership of any RRs
        ///   is required. RDLENGTH is zero and RDATA is therefore empty. CLASS
        ///   must be specified as NONE. TYPE must be specified as ANY. TTL must
        ///   be specified as zero (0).
        /// </remarks>
        public UpdatePrerequisiteList MustNotExist(DomainName name)
        {
            return MustNotExist(name, DnsType.ANY);
        }

        /// <summary>
        ///   No resource record with the specified name and type can exist
        ///   in the <see cref="UpdateMessage.Zone"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   A derived class of <see cref="ResourceRecord"/>.
        /// </typeparam>
        /// <param name="name">A resource name.</param>
        /// <returns>
        ///   The prerequisite list to allow fluent usage.
        /// </returns>
        /// <remarks>
        ///   For this prerequisite, a requestor adds to the section a single RR
        ///   whose NAME and TYPE are equal to that of the RRset whose nonexistence
        ///   is required. The RDLENGTH of this record is zero (0), and RDATA
        ///   field is therefore empty.  CLASS must be specified as NONE in order
        ///   to distinguish this condition from a valid RR whose RDLENGTH is
        ///   naturally zero (0) (for example, the NULL RR).  TTL must be specified
        ///   as zero(0).
        /// </remarks>
        public UpdatePrerequisiteList MustNotExist<T>(DomainName name)
            where T : ResourceRecord, new()
        {
            return MustNotExist(name, new T().Type);
        }
    }
}
