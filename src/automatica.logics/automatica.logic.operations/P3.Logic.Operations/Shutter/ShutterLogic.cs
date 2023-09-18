using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Shutter
{
    public class ShutterLogic: Automatica.Core.Logic.Logic
    {
      

        public ShutterLogic(ILogicContext context) : base(context)
        {
          ;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
          
            return new List<ILogicOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
