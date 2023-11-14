using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.TextToSpeech
{
    public class MessengerLogic: Automatica.Core.Logic.Logic
    {
       
        private object _value;

        public MessengerLogic(ILogicContext context) : base(context)
        {
        
            
        }

        protected override async Task<bool> Start(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            var url = await Context.CloudApi.Synthesize(Context.RuleInstance.ObjId, "This is very awesome", "en-US", "JennyNeural");
            return true;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {

           

            _value = value;
            return base.InputValueChanged(instance, source, value);
        }
    }
}
