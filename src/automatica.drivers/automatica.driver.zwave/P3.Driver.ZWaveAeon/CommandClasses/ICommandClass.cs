using P3.Driver.ZWaveAeon.Channel;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public interface ICommandClass
    {
        Node Node { get; }
        CommandClass Class { get; }
    }
}
