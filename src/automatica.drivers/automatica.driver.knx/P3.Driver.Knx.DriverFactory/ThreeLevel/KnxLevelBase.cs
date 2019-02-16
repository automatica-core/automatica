using Automatica.Core.Driver;
using P3.Knx.Core.Driver;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public abstract class KnxLevelBase : DriverBase
    {
        public KnxConnection Tunneling => Driver.Tunneling;
        public KnxDriver Driver { get; }
        public int Address { get; private set; }

        private readonly IList<KnxLevelBase> _children;


        protected KnxLevelBase(IDriverContext driverContext,  KnxDriver driver) : base(driverContext)
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

        public override bool Init()
        {
            Address = GetPropertyValueInt("knx-address");
            return base.Init();
        }
    }
}
