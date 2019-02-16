using System;
using System.Collections.Generic;

namespace Automatica.Core.EF.Models.AutomaticDesigner
{
    public class AutomaticGroup
    {
        public Guid ObjId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }


        //overwrites rule condition
        public List<AutomaticCondition> Conditions { get; set; }


        public bool ExecuteActionsDelayed { get; set; }

        public bool DelayRandom { get; set; }
        public int DelayFrom { get; set; }
        public int DelayTo { get; set; }

        public List<AutomaticRule> Rules { get; set; }
    }
}
