using Automatica.Core.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.Nuki.Driver.Model;

namespace P3.Driver.Nuki.Driver
{
    internal class NukiSmartLockAttribute : DriverNotWriteableBase
    {
        public Func<NukiObject, object> GetValue { get; }

        public NukiSmartLockAttribute(IDriverContext driverContext, Func<NukiObject, object> getValue) : base(driverContext)
        {
            GetValue = getValue;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Parent.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
