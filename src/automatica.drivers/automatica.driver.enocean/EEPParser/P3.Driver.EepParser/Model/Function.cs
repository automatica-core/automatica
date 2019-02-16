using System;
using System.Collections.Generic;

namespace P3.Driver.EepParser.Model
{
    public class Function : BaseModel
    {
        public Rorg Parent { get; }
        public string Number { get; set; }
        public string Title { get; set; }

        public List<Type> Types { get; set; }

        public Function(Rorg parent, string number)
        {
            Parent = parent;
            Number = number;
            Types = new List<Type>();
        }
    }
}
