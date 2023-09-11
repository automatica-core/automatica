using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Gauge.EasyLinear
{
    public class EasyLinearControl : Automatica.Core.Logic.Logic
    {
        public EasyLinearControl(ILogicContext context) : base(context)
        {
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(true);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(true);
        }
    }
}
