using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.LoxApp
{
    public class LoxoneCategory
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int DefaultRating { get; set; }
        public bool IsFavorite { get; set; }

        public string Type { get; set; }
        public string Color { get; set; }
    }
}