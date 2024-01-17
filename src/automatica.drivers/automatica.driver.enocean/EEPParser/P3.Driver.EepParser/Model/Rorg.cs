using System.Collections.Generic;

namespace P3.Driver.EepParser.Model
{
    public class Rorg : BaseModel
    {
        public string Number { get; set; }
        public string Title { get; set; }
        public string FullName { get; set; }

        public List<Function> Functions { get; set; }

        public Rorg(string number)
        {
            Number = number;
            Functions = new List<Function>();
        }
    }
}
