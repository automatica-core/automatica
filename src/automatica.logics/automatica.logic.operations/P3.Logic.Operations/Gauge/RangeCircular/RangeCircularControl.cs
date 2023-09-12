using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Gauge.RangeCircular
{
    public class RangeCircularControl : Automatica.Core.Logic.Logic
    {
        public RangeCircularControl(ILogicContext context) : base(context)
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
