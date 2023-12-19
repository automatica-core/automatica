namespace Automatica.Core.Control.Base
{
    public interface IDimmer : ISwitch
    {
        public Guid DimmerOutputValueId { get; set; }
        public Guid DimmerInputValueId { get; set; }
    }
}
