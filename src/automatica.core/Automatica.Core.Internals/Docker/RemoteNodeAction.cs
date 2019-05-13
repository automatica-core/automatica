namespace Automatica.Core.Internals.Docker
{
    public enum SlaveAction
    {
        Start,
        Stop,
        Restart,
        Kill
    }

    public class RemoteNodeActionRequest
    {
        public SlaveAction Action { get; set; }
        public string ImageName { get; set; }
        public string Tag { get; set; }
        public string ImageSource { get; set; }
    }
}
