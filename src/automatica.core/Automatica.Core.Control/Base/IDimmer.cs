namespace Automatica.Core.Control.Base
{
    public interface IDimmer : ISwitch
    {
        public Guid DimmerOutputValueId { get;  }
        public Guid DimmerInputValueId { get; }

        public bool DimmerState { get; }
        public int DimmerValue { get; }
        
        public Task<bool> DimAsync(int value, CancellationToken cancellationToken = default);
    }
}
