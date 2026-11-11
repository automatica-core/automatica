using System.Collections.Generic;

namespace P3.Driver.EepParser.Model
{
    public class Function : BaseModel
    {
        public Rorg Parent { get; }
        public string Number { get; set; }
        public string Title { get; set; }

        public List<Type> Types { get; set; }

        /// <summary>
        /// Bidirectional devices generated as one template: status case and command case of the same EEP type.
        /// </summary>
        public List<TwoWayType> TwoWays { get; } = new List<TwoWayType>();

        public Function(Rorg parent, string number)
        {
            Parent = parent;
            Number = number;
            Types = new List<Type>();
        }
    }
}
