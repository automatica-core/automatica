using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.HueBridge.Data
{
    public class HueUser
    {
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUsed { get; set; }
    }
}
