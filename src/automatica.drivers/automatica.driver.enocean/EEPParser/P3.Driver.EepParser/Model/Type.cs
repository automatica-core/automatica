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
}
