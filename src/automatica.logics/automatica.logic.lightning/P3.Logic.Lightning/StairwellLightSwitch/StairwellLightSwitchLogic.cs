using System;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.StairwellLightSwitch
{
    /// <summary>
    /// Every 1 on Tr (re)starts the on duration; a permanent light (On) ignores Tr until Off
    /// </summary>
    public class StairwellLightSwitchLogic : TimedSwitchLogic
    {
        public StairwellLightSwitchLogic(ILogicContext context) : base(context, StairwellLightSwitchLogicFactory.Ids)
        {
        }

        protected override void OnTrigger(Guid template, object value, DateTime now)
        {
            if (template == StairwellLightSwitchLogicFactory.Trigger && LightingValues.IsTruthy(value))
            {
                SwitchOnTimed(now);
            }
        }
    }
}
