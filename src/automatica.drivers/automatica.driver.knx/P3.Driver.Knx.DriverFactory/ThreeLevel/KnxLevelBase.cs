using Automatica.Core.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public abstract class KnxLevelBase : DriverBase
    {
        public KnxDriver Driver { get; }
        public int Address { get; private set; }

        private readonly IList<KnxLevelBase> _children;


        protected KnxLevelBase(IDriverContext driverContext, KnxDriver driver) : base(driverContext)
        {
            Driver = driver;
            _children = new List<KnxLevelBase>();
        }

        internal virtual void ConnectionEstablished()
        {
            foreach (var m in _children)
            {
                m.ConnectionEstablished();
            }
        }

        protected void AddChild(KnxLevelBase b)
        {
            _children.Add(b);
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            Address = GetPropertyValueInt("knx-address");
            return base.Init(token);
        }
    }
}
