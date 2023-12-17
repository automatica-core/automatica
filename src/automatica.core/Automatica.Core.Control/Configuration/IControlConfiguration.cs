using Automatica.Core.Control.Base;

namespace Automatica.Core.Control.Configuration
{
    public interface IControlConfiguration
    {
        List<ISwitch> Switches { get; }
        List<IDimmer> Dimmer { get;} 
        List<IBlind> Blinds { get; }

    }
}
