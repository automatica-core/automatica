using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering
{
    public class MeteringLogic : Automatica.Core.Logic.Logic
    {
       
    
        public MeteringLogic(ILogicContext context) : base(context)
        {
        
            
        }

        protected override async Task<bool> Start(RuleInstance instance,
            CancellationToken token = new CancellationToken())
        {
          

            await Task.CompletedTask;

            return true;
        }

     

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            
            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var ret = new List<ILogicOutputChanged>();
           
            return ret;
        }
    }
}
