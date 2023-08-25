using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.SonosDriverFactory.Attributes
{
    internal class SonosStatusValueAttribute : DriverNotWriteableBase
    {
        private readonly Func<GetPositionInfoResponse, object> _getFunc;

        public SonosStatusValueAttribute(IDriverContext driverContext,  Func<GetPositionInfoResponse, object> getFunc) : base(driverContext)
        {
            _getFunc = getFunc;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Parent.Read(token);
        }

     
        internal void GetValueAndDispatch(GetPositionInfoResponse info)
        {
            var value = _getFunc(info);
            DispatchRead(value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
