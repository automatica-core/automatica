using System.Collections.Generic;

namespace P3.Driver.EepParser.Model
{
    public class Type : BaseModel
    {
        public Function Parent { get; }
        public string Number { get; set; }
        public string OrigTypeNumber { get; set; }

        public int TypeIndex { get; set; }
        public string Title { get; set; }

        public Dictionary<string, DataField> DataFields { get; set; }

        /// <summary>
        /// Data field values a telegram must carry to belong to this case (e.g. D2-01 CMD = 0x4), empty = every telegram.
        /// </summary>
        public List<CaseCondition> Conditions { get; } = new List<CaseCondition>();

        /// <summary>
        /// Title of the EEP case (e.g. "CMD 0x1 - Actuator Set Output").
        /// </summary>
        public string CaseTitle { get; set; }

        /// <summary>
        /// The case is a command Automatica sends to the device (actuator set, query, ...).
        /// </summary>
        public bool Outgoing { get; set; }

        /// <summary>
        /// User data length of an outgoing telegram in bytes.
        /// </summary>
        public int TelegramSize { get; set; }

        /// <summary>
        /// Field values of an outgoing telegram before the written values are set: case constants, "do not change" /
        /// "all channels" values and the LRN bit of 4BS data telegrams.
        /// </summary>
        public List<CaseCondition> WriteDefaults { get; } = new List<CaseCondition>();

        /// <summary>
        /// Template of the teach-in data point of an outgoing case.
        /// </summary>
        public System.Guid TeachInGuid { get; set; }

        public Type(Function parent, string number)
        {
            Parent = parent;
            Number = number;
            DataFields = new Dictionary<string, DataField>();
        }

        public string TypeId()
        {
            return $"{Parent.Parent.Number}_{Parent.Number}_{Number}";
        }
    }

    public class CaseCondition
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public long Value { get; set; }

        public override bool Equals(object obj) =>
            obj is CaseCondition c && c.Offset == Offset && c.Length == Length && c.Value == Value;

        public override int GetHashCode() => (Offset, Length, Value).GetHashCode();
    }
}
