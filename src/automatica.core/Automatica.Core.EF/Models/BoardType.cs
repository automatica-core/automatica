using System;
using System.Collections.Generic;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class BoardType : TypedObject
    {
        public BoardType()
        {
            BoardInterface = new List<BoardInterface>();
        }

        public Guid Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<BoardInterface> BoardInterface { get; set; }
    }
}
