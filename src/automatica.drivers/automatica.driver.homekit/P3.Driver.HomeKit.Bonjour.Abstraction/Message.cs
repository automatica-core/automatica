using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   All communications inside of the domain protocol are carried in a single
    ///   format called a message.
    /// </summary>
    public class Message : DnsObject
    {
        /// <summary>
        ///   The least significant 4 bits of the opcode.
        /// </summary>
        byte opcode4;

        /// <summary>
        ///   Maximum bytes of a message.
        /// </summary>
        /// <value>
        ///   9000 bytes.
        /// </value>
        /// <remarks>
        ///   In reality the max length is dictated by the network MTU.  For legacy IPv4 systems,
        ///   512 bytes should be used.  For DNSSEC, at least 4096 bytes are needed.
        ///   <para>
        ///   9000 bytes (less IP and UPD header lengths) is specified by Multicast DNS.
        ///   </para>
        /// </remarks>
        public const int MaxLength = 9000;

        /// <summary>
        ///   Minimum bytes of a messages
        /// </summary>
        /// <value>
        ///   12 bytes.
        /// </value>
        public const int MinLength = 12;

        /// <summary>
        /// A 16 bit identifier assigned by the program that
        /// generates any kind of query. This identifier is copied
        /// the corresponding reply and can be used by the requester
        /// to match up replies to outstanding queries.
        /// </summary>
        /// <value>
        ///   A unique identifier.
        /// </value>
        public ushort Id { get; set; }

        /// <summary>
        ///   A one bit field that specifies whether this message is a query(0), or a response(1).
        /// </summary>
        /// <value>
        ///   <b>false</b> for a query; otherwise, <b>true</b> for a response.
        /// </value>
        public bool QR { get; set; }

        /// <summary>
        ///   Determines if the message is query.
        /// </summary>
        /// <value>
        ///   <b>true</b> for a query; otherwise, <b>false</b> for a response.
        /// </value>
        public bool IsQuery { get { return !QR; } }

        /// <summary>
        ///   Determines if the message is a response to a query.
        /// </summary>
        /// <value>
        ///   <b>false</b> for a query; otherwise, <b>true</b> for a response.
        /// </value>
        public bool IsResponse { get { return QR; } }


        /// <summary>
        ///   The requested operation. 
        /// </summary>
        /// <value>
        ///   One of the <see cref="MessageOperation"/> values. Both standard
        ///   and extended values are supported.
        /// </value>
        /// <remarks>
        ///   This value is set by the originator of a query
        ///   and copied into the response.
        ///   <para>
        ///   Extended opcodes (values requiring more than 4 bits) are split between
        ///   the message header and the <see cref="OPTRecord"/> in the
        ///   <see cref="AdditionalRecords"/> section.  When setting an extended opcode,
        ///   the <see cref="OPTRecord"/> will be created if it does not already
        ///   exist.
        ///   </para>
        /// </remarks>
        /// <seealso cref="Message.CreateResponse"/>
        public MessageOperation Opcode
        {
            get
            {
                var opt = AdditionalRecords.OfType<OPTRecord>().FirstOrDefault();
                if (opt == null)
                    return (MessageOperation)opcode4;
                return (MessageOperation)(((ushort)opt.Opcode8 << 4) | opcode4);
            }
            set
            {
                var opt = AdditionalRecords.OfType<OPTRecord>().FirstOrDefault();

                // Is standard opcode?
                var extendedOpcode = (int)value;
                if ((extendedOpcode & 0xff0) == 0)
                {
                    opcode4 = (byte)extendedOpcode;
                    if (opt != null)
                        opt.Opcode8 = 0;
                    return;
                }

                // Extended opcode, needs an OPT resource record.
                if (opt == null)
                {
                    opt = new OPTRecord();
                    AdditionalRecords.Add(opt);
                }
                opcode4 = (byte)(extendedOpcode & 0xf);
                opt.Opcode8 = (byte)((extendedOpcode >> 4) & 0xff);
            }
        }

        /// <summary>
        ///    Authoritative Answer - this bit is valid in responses,
        ///    and specifies that the responding name server is an
        ///    authority for the domain name in question section.
        ///    
        ///    Note that the contents of the answer section may have
        ///    multiple owner names because of aliases.The AA bit
        ///    corresponds to the name which matches the query name, or
        ///    the first owner name in the answer section.
        /// </summary>
        /// <value>
        ///   <b>true</b> for an authoritative answer; otherwise, <b>false</b>.
        /// </value>
        public bool AA { get; set; }

        /// <summary>
        ///   TrunCation - specifies that this message was truncated
        ///   due to length greater than that permitted on the
        ///   transmission channel.
        /// </summary>
        /// <value>
        ///   <b>true</b> for a truncated message; otherwise, <b>false</b>.
        /// </value>
        /// <seealso cref="Truncate(int)"/>
        public bool TC { get; set; }

        /// <summary>
        ///    Recursion Desired - this bit may be set in a query and
        ///    is copied into the response. If RD is set, it directs
        ///    the name server to pursue the query recursively.
        ///    
        ///    Recursive query support is optional.
        /// </summary>
        /// <value>
        ///   <b>true</b> if recursion is desired; otherwise, <b>false</b>.
        /// </value>
        public bool RD { get; set; }

        /// <summary>
        ///    Recursion Available - this be is set or cleared in a
        ///    response, and denotes whether recursive query support is
        ///    available in the name server.
        /// </summary>
        /// <value>
        ///   <b>true</b> if recursion is available; otherwise, <b>false</b>.
        /// </value>
        public bool RA { get; set; }

        /// <summary>
        ///    Reserved for future use. 
        /// </summary>
        /// <value>
        ///    Must be zero in all queries and responses.
        /// </value>
        public int Z { get; set; }

        /// <summary>
        ///   Authentic data.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the response data is authentic; otherwise, <b>false</b>.
        /// </value>
        /// <remarks>
        ///   Only used in a response and indicates that
        ///   all the data included in the <see cref="Answers"/> and 
        ///   <see cref="AuthorityRecords"/> sections are authenticated by the 
        ///   server according to its DNSSEC policies.
        /// </remarks>
        public bool AD { get; set; }

        /// <summary>
        ///   Checking disabled.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the query does not require 
        ///   <see cref="AD">authenticated data</see>; otherwise, <b>false</b>.
        /// </value>
        /// <remarks>
        ///   Only used in a query and indicates that pending (non-authenticated) 
        ///   data is acceptable to the resolver sending the query.
        /// </remarks>
        public bool CD { get; set; }

        /// <summary>
        ///   Indicates that DNS Security Extensions (DNSSEC) are supported.
        /// </summary>
        /// <value>
        ///   <b>true</b> if DNSSEC is supported; otherwise, <b>false</b>.
        /// </value>
        /// <remarks>
        ///   The <b>DO</b> bit is actually in the <see cref="OPTRecord"/>, when setting
        ///   the record is added to <see cref="AdditionalRecords"/> if not already present.
        /// </remarks>
        /// <seealso cref="UseDnsSecurity"/>
        /// <seealso href="https://tools.ietf.org/html/rfc3225"/>
        public bool DO
        {
            get
            {
                var opt = AdditionalRecords.OfType<OPTRecord>().FirstOrDefault();
                return opt?.DO ?? false;
            }
            set
            {
                var opt = AdditionalRecords.OfType<OPTRecord>().FirstOrDefault();
                if (opt == null)
                {
                    opt = new OPTRecord();
                    AdditionalRecords.Add(opt);
                }
                opt.DO = value;
            }
        }

        /// <summary>
        ///     Response code - this 4 bit field is set as part of responses.
        /// </summary>
        /// <value>
        ///   One of the <see cref="MessageStatus"/> values.
        /// </value>
        public MessageStatus Status { get; set; }

        /// <summary>
        ///   The list of question.
        /// </summary>
        /// <value>
        ///   A list of questions.
        /// </value>
        public List<Question> Questions { get; } = new List<Question>();

        /// <summary>
        ///   The list of answers.
        /// </summary>
        /// <value>
        ///   A list of answers.
        /// </value>
        public List<ResourceRecord> Answers { get; set; } = new List<ResourceRecord>();

        /// <summary>
        ///   The list of authority records.
        /// </summary>
        /// <value>
        ///   A list of authority resource records.
        /// </value>
        public List<ResourceRecord> AuthorityRecords { get; set;  } = new List<ResourceRecord>();

        /// <summary>
        ///   The list of additional records.
        /// </summary>
        /// <value>
        ///   A list of additional resource records.
        /// </value>
        public List<ResourceRecord> AdditionalRecords { get; set;  } = new List<ResourceRecord>();

        /// <summary>
        ///   Create a response for the query message.
        /// </summary>
        /// <returns>
        ///   A new response for the query message.
        /// </returns>
        public Message CreateResponse()
        {
            var response = new Message
            {
                Id = Id,
                Opcode = Opcode,
                QR = true,
            };
            response.Questions.AddRange(Questions);
            return response;
        }

        /// <summary>
        ///   Make the message not exceed the specified length.
        /// </summary>
        /// <param name="length">
        ///   The maximum number bytes for the message.
        /// </param>
        /// <remarks>
        ///   If the message does not fit into <paramref name="length"/> bytes, then <see cref="AdditionalRecords"/>
        ///   are removed and then <see cref="AuthorityRecords"/> are removed.  
        ///   <para>
        ///   If it is still too big, then the <see cref="TC"/> bit is set.
        ///   </para>
        /// </remarks>
        public void Truncate(int length)
        {
            while (Length() > length)
            {
                // Remove records.
                if (AdditionalRecords.Count > 0)
                {
                    AdditionalRecords.RemoveAt(AdditionalRecords.Count - 1);
                }
                else if (AuthorityRecords.Count > 0)
                {
                    AuthorityRecords.RemoveAt(AuthorityRecords.Count - 1);
                }
                else
                {
                    // Nothing more can be done to reduce the message length.
                    TC = true;
                    return;
                }
            }
        }

        /// <summary>
        ///   Enables DNS Security Extensions (DNSSEC) for the message.
        /// </summary>
        /// <returns>
        ///   The <see cref="Message"/> for a fluent design.
        /// </returns>
        /// <remarks>
        ///   Sets <see cref="OPTRecord.DO"/> to <b>true</b>.  Adds an <see cref="OPTRecord"/> to
        ///   <see cref="AdditionalRecords"/> if not already present.
        /// </remarks>
        /// <seealso cref="DO"/>
        public Message UseDnsSecurity()
        {
            DO = true;
            return this;
        }

        /// <inheritdoc />
        public override IWireSerialiser Read(WireReader reader)
        {
            Id = reader.ReadUInt16();
            var flags = reader.ReadUInt16();
            QR = (flags & 0x8000) == 0x8000;
            AA = (flags & 0x0400) == 0x0400;
            TC = (flags & 0x0200) == 0x0200;
            RD = (flags & 0x0100) == 0x0100;
            RA = (flags & 0x0080) == 0x0080;
            opcode4 = (byte)((flags & 0x7800) >> 11);
            Z = (flags & 0x0040) >> 6;
            AD = (flags & 0x0020) == 0x0020;
            CD = (flags & 0x0010) == 0x0010;
            Status = (MessageStatus)(flags & 0x000F);
            var qdcount = reader.ReadUInt16();
            var ancount = reader.ReadUInt16();
            var nscount = reader.ReadUInt16();
            var arcount = reader.ReadUInt16();
            for (var i = 0; i < qdcount; ++i)
            {
                var question = (Question) new Question().Read(reader);
                Questions.Add(question);
            }
            for (var i = 0; i < ancount; ++i)
            {
                var rr = (ResourceRecord) new ResourceRecord().Read(reader);
                Answers.Add(rr);
            }
            for (var i = 0; i < nscount; ++i)
            {
                var rr = (ResourceRecord)new ResourceRecord().Read(reader);
                AuthorityRecords.Add(rr);
            }
            for (var i = 0; i < arcount; ++i)
            {
                var rr = (ResourceRecord)new ResourceRecord().Read(reader);
                AdditionalRecords.Add(rr);
            }

            return this;
        }

        /// <inheritdoc />
        public override void Write(WireWriter writer)
        {
            writer.WriteUInt16(Id);
            var flags =
                (Convert.ToInt32(QR) << 15) |
                (((ushort)opcode4 & 0xf)<< 11) |
                (Convert.ToInt32(AA) << 10) |
                (Convert.ToInt32(TC) << 9) |
                (Convert.ToInt32(RD) << 8) |
                (Convert.ToInt32(RA) << 7) |
                ((Z & 0x1) << 6) |
                (Convert.ToInt32(AD) << 5) |
                (Convert.ToInt32(CD) << 4) |
                ((ushort)Status & 0xf);
            writer.WriteUInt16((ushort)flags);
            writer.WriteUInt16((ushort)Questions.Count);
            writer.WriteUInt16((ushort)Answers.Count);
            writer.WriteUInt16((ushort)AuthorityRecords.Count);
            writer.WriteUInt16((ushort)AdditionalRecords.Count);
            foreach (var r in Questions) r.Write(writer);
            foreach (var r in Answers) r.Write(writer);
            foreach (var r in AuthorityRecords) r.Write(writer);
            foreach (var r in AdditionalRecords) r.Write(writer);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            using (var s = new StringWriter())
            {
                s.Write(";; Header:");
                if (QR) s.Write(" QR");
                if (AA) s.Write(" AA");
                if (TC) s.Write(" TC");
                if (RD) s.Write(" RD");
                if (AD) s.Write(" AD");
                if (CD) s.Write(" CD");
                s.Write(" RCODE=");
                s.Write(Status);
                s.WriteLine();

                s.WriteLine();
                s.WriteLine(";; Question");
                if (Questions.Count == 0)
                {
                    s.WriteLine(";;  (empty)");
                }
                else
                {
                    foreach (var q in Questions)
                    {
                        s.WriteLine(q.ToString());
                    }
                }

                Stringify(s, "Answer", Answers);
                Stringify(s, "Authority", AuthorityRecords);
                Stringify(s, "Additional", AdditionalRecords);

                return s.ToString();
            }
        }

        void Stringify(StringWriter s, string title, List<ResourceRecord> records)
        {
            s.WriteLine();
            s.Write(";; ");
            s.WriteLine(title);
            if (records.Count == 0)
            {
                s.WriteLine(";;  (empty)");
            }
            else
            {
                foreach (var r in records)
                {
                    s.WriteLine(r.ToString());
                }
            }
        }
    }
}
