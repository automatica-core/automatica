using Automatica.Core.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.Nuki.Driver.Model;

namespace P3.Driver.Nuki.Driver
{
    internal class NukiSmartLock : DriverNoneAttributeBase
    {
        public int NukiId { get; set; }

        private readonly List<NukiSmartLockAttribute> _attributes = new();

        public NukiSmartLock(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = new CancellationToken())
        {
            NukiId = GetPropertyValueInt("id");
            return base.Init(token);
        }

        public void UpdateState(NukiObject nuki)
        {
            foreach (var attr in _attributes)
            {
                var value = attr.GetValue(nuki);
                attr.DispatchRead(value);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            NukiSmartLockAttribute attr = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "state":
                    attr = new NukiSmartLockAttribute(ctx, o => o.LastKnownState.State);
                    break;
                case "stateName":
                    attr = new NukiSmartLockAttribute(ctx, o => o.LastKnownState.StateName);
                    break;
                case "batteryCritical":
                    attr = new NukiSmartLockAttribute(ctx, o => o.LastKnownState.BatteryCritical);
                    break;
                case "batteryChargeState":
                    attr = new NukiSmartLockAttribute(ctx, o => o.LastKnownState.BatteryChargeState);
                    break;
                case "timestamp":
                    attr = new NukiSmartLockAttribute(ctx, o => o.LastKnownState.Timestamp);
                    break;
            }

            if(attr != null)
                _attributes.Add(attr);

            return attr;
        }

    }
}
