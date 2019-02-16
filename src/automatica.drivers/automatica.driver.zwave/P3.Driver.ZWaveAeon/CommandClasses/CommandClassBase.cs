using P3.Driver.ZWaveAeon.Channel;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class CommandClassBase : ICommandClass
    {
        public Node Node { get; private set; }
        public CommandClass Class { get; private set; }

        public CommandClassBase(Node node, CommandClass @class)
        {
            Node = node;
            Class = @class;
        }

        protected ZWaveChannel Channel
        {
            get { return Node.Controller.Channel; }
        }

        internal protected virtual void HandleEvent(Command command)
        {
        }
    }
}
