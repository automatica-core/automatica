using System;
using P3.Driver.ZWaveAeon.Channel;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class SceneActivation : CommandClassBase
    {
        public event EventHandler<ReportEventArgs<SceneActivationReport>> Changed;

        enum command : byte
        {
            Set = 0x01,
        }

        public SceneActivation(Node node) : base(node, CommandClass.SceneActivation)
        {
        }

        protected internal override void HandleEvent(Command command)
        {
            base.HandleEvent(command);

            if (command.CommandID == Convert.ToByte(SceneActivation.command.Set))
            {
                var report = new SceneActivationReport(Node, command.Payload);
                OnChanged(new ReportEventArgs<SceneActivationReport>(report));
            }
        }

        protected virtual void OnChanged(ReportEventArgs<SceneActivationReport> e)
        {
            Changed?.Invoke(this, e);
        }
    }
}