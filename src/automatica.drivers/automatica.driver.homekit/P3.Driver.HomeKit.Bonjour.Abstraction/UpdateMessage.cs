using System;
using System.Collections.Generic;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Dynamic updates in the Domain Name System.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   <see href="https://tools.ietf.org/html/rfc2136">RFC 2136</see> allows adding or 
    ///   deleting resource records from a specified zone.
    /// </para>
    /// <para>
    ///   <see cref="Prerequisites"/> are  specified separately from 
    ///   <see cref="Updates">update operations</see>, and can specify a
    ///   dependency upon either the previous existence or nonexistence of an
    ///   RRset, or the existence of a single RR.
    /// </para>
    /// <para>
    ///   An update is atomic, i.e., all prerequisites must be satisfied or else
    ///   no update operations will take place. There are no data dependent
    ///   error conditions defined after the prerequisites have been met.
    /// </para>
    /// </remarks>
    /// <seealso href="https://tools.ietf.org/html/rfc2136"/>
    public class UpdateMessage : DnsObject
    {
        /// <summary>
        ///   A 16 bit identifier assigned by the program that
        ///   generates any kind of update. 
        /// </summary>
        /// <value>
        ///   A unique identifier assigned by the requestor.
        /// </value>
        /// <remarks>
        ///   This identifier is copied to
        ///   the corresponding response and can be used by the requestor
        ///   to match up replies to outstanding queries.
        /// </remarks>
        public ushort Id { get; set; }

        /// <summary>
        ///   Determines if the message is a request or a response.
        /// </summary>
        /// <value>
        ///   0 if the message is a request or 1 if the message is a response.
        /// </value>
        /// <seealso cref="IsUpdate"/>
        /// <seealso cref="IsResponse"/>
        public bool QR { get; set; }

        /// <summary>
        ///   Determines if the message is an update.
        /// </summary>
        /// <value>
        ///   <b>true</b> if <see cref="QR"/> is <b>false</b>.
        /// </value>
        public bool IsUpdate { get { return !QR; } }

        /// <summary>
        ///   Determines if the message is a response to an update.
        /// </summary>
        /// <value>
        ///   <b>true</b> if <see cref="QR"/> is <b>true</b>.
        /// </value>
        public bool IsResponse { get { return QR; } }


        /// <summary>
        ///   The kind of message.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="MessageOperation.Update"/>.
        /// </value>
        public MessageOperation Opcode { get; set; } = MessageOperation.Update;

        /// <summary>
        ///   Reserved for future use.  Must be zero in all updates
        ///   and responses.
        /// </summary>
        /// <value>
        ///   Must be zero.
        /// </value>
        public int Z { get; set; }

        /// <summary>
        ///   Response code - this 4 bit field is set as part of responses.
        /// </summary>
        /// <value>
        ///   One of the <see cref="MessageStatus"/> values.
        /// </value>
        public MessageStatus Status { get; set; }

        /// <summary>
        ///   The zone to update.
        /// </summary>
        /// <value>
        ///   Defaults to the empty zone.  <see cref="Question.Name"/> is <b>null</b>,
        ///   <see cref="Question.Class"/> is <see cref="DnsClass.IN"/> and
        ///   <see cref="Question.Type"/> is SOA (6).
        /// </value>
        public Question Zone { get; set; } = new Question
        {
            Class = DnsClass.IN,
            Type = DnsType.SOA
        };

        /// <summary>
        ///   Resource records which must (not) preexist.
        /// </summary>
        /// <value>
        ///   Defaults to an empty list.
        /// </value>
        public UpdatePrerequisiteList Prerequisites { get; } = new UpdatePrerequisiteList();

        /// <summary>
        ///   Resource records to be added or deleted.
        /// </summary>
        /// <value>
        ///   Defaults to an empty list.
        /// </value>
        public UpdateResourceList Updates { get; } = new UpdateResourceList();

        /// <summary>
        ///   The list of additional resource records.
        /// </summary>
        /// <value>
        ///   Defaults to an empty list.
        /// </value>
        /// <remarks>
        ///   The resources which are related to the update itself, or
        ///   to new resources being added by the update. For example, out of zone glue
        ///   (A RRs referred to by new NS RRs) should be presented here.
        ///   <para>
        ///   The  server can use or ignore out of zone glue, at the discretion of the
        ///   server implementor.
        ///   </para>
        /// </remarks>
        public List<ResourceRecord> AdditionalResources { get; } = new List<ResourceRecord>();

        /// <summary>
        ///   Create a response for the update message.
        /// </summary>
        /// <returns></returns>
        public UpdateMessage CreateResponse()
        {
            return new UpdateMessage
            {
                Id = Id,
                Opcode = Opcode,
                QR = true
            };
        }

        /// <inheritdoc />
        public override IWireSerialiser Read(WireReader reader)
        {
            Id = reader.ReadUInt16();
            var flags = reader.ReadUInt16();
            QR = (flags & 0x8000) == 0x8000;
            Opcode = (MessageOperation)((flags & 0x7800) >> 11);
            Z = (flags & 0x07F0) >> 4;
            Status = (MessageStatus)(flags & 0x000F);
            var zocount = reader.ReadUInt16();
            var prcount = reader.ReadUInt16();
            var upcount = reader.ReadUInt16();
            var arcount = reader.ReadUInt16();
            for (var i = 0; i < zocount; ++i)
            {
                Zone = (Question) new Question().Read(reader);
            }
            for (var i = 0; i < prcount; ++i)
            {
                var rr = (ResourceRecord) new ResourceRecord().Read(reader);
                Prerequisites.Add(rr);
            }
            for (var i = 0; i < upcount; ++i)
            {
                var rr = (ResourceRecord)new ResourceRecord().Read(reader);
                Updates.Add(rr);
            }
            for (var i = 0; i < arcount; ++i)
            {
                var rr = (ResourceRecord)new ResourceRecord().Read(reader);
                AdditionalResources.Add(rr);
            }

            return this;
        }

        /// <inheritdoc />
        public override void Write(WireWriter writer)
        {
            writer.WriteUInt16(Id);
            var flags =
                (Convert.ToInt32(QR) << 15) |
                (((ushort)Opcode & 0xf) << 11) |
                ((Z & 0x7F) << 4) |
                ((ushort)Status & 0xf);
            writer.WriteUInt16((ushort)flags);
            writer.WriteUInt16((ushort)1);
            writer.WriteUInt16((ushort)Prerequisites.Count);
            writer.WriteUInt16((ushort)Updates.Count);
            writer.WriteUInt16((ushort)AdditionalResources.Count);
            Zone.Write(writer);
            foreach (var r in Prerequisites) r.Write(writer);
            foreach (var r in Updates) r.Write(writer);
            foreach (var r in AdditionalResources) r.Write(writer);
        }
    }
}
