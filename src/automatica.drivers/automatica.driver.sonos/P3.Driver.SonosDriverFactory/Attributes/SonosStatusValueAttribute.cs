using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.SonosDriverFactory.Attributes
{
    internal class SonosStatusValueAttribute : DriverBase
    {
        private readonly Func<GetPositionInfoResponse, object> _getFunc;

        public SonosStatusValueAttribute(IDriverContext driverContext,  Func<GetPositionInfoResponse, object> getFunc) : base(driverContext)
        {
            _getFunc = getFunc;
        }

        public override Task<bool> Read(CancellationToken token = new CancellationToken())
        {
            return Parent.Read(token);
        }

        internal void GetValueAndDispatch(GetPositionInfoResponse info)
        {
            var value = _getFunc(info);
            DispatchValue(value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
