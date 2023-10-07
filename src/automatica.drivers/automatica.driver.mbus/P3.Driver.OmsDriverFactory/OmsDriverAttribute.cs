using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Frames.VariableData;

namespace P3.Driver.OmsDriverFactory
{
    public class OmsDriverAttribute : DriverNotWriteableBase
    {
        private readonly int _dataIndex;
        private VariableDataBlock _lastFrame;

        public OmsDriverAttribute(IDriverContext driverContext, int dataIndex) : base(driverContext)
        {
            _dataIndex = dataIndex;
        }

        public void SetData(VariableDataFrame data)
        {
            if (data.DataBlocks.Count >= _dataIndex)
            {
                var frame = data.DataBlocks[_dataIndex];

                DriverContext.Logger.LogDebug($"{_dataIndex}{Name} value {frame.Value}");

                if (_lastFrame != null && _lastFrame.Value != frame.Value)
                {
                    DispatchRead(frame.Value);
                }

                _lastFrame = frame;
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Parent.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
