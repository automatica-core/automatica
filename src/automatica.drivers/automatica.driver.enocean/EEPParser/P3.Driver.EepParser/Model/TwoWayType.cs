using System;
using System.Collections.Generic;

namespace P3.Driver.EepParser.Model
{
    /// <summary>
    /// One template for a bidirectional device (A5-20 HVAC components): the data fields of the status case (device to
    /// Automatica, read only) and of the command case (Automatica to device, writable) below one node.
    /// </summary>
    public class TwoWayType
    {
        public TwoWayType(Type status, Type command)
        {
            Status = status;
            Command = command;
        }

        public Type Status { get; }
        public Type Command { get; }

        /// <summary>
        /// The type number without case, e.g. 01 for A5-20-01.
        /// </summary>
        public string Number => Status.OrigTypeNumber.Replace("0x", "");

        public Guid Guid { get; set; }

        public List<TwoWayField> Fields { get; } = new List<TwoWayField>();
    }

    public class TwoWayField
    {
        public TwoWayField(DataField original, Type originalType, bool command, Guid guid)
        {
            Original = original;
            OriginalType = originalType;
            Command = command;
            Guid = guid;
        }

        public DataField Original { get; }
        public Type OriginalType { get; }

        /// <summary>
        /// True for a field of the command case: writable, sent as answer to the next status telegram.
        /// </summary>
        public bool Command { get; }

        public Guid Guid { get; }

        /// <summary>
        /// IN (status) or OUT (command), part of the generated names.
        /// </summary>
        public string Direction => Command ? "OUT" : "IN";
    }
}
