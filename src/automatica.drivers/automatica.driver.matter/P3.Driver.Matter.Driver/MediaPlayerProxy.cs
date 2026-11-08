using System.Reflection;
using Automatica.Core.Control.Base;

namespace P3.Driver.Matter.Driver
{
    /// <summary>
    /// Reaches a core <c>Automatica.Core.Control.Base.IMediaPlayer</c> by name. The plugin builds against the published core
    /// NuGet, which does not have the interface yet; on a core that has it, media player controls are bridged, on older
    /// cores there are none.
    /// </summary>
    internal sealed class MediaPlayerProxy
    {
        internal const string InterfaceName = "Automatica.Core.Control.Base.IMediaPlayer";

        // MediaPlaybackState.Playing and MediaPlayerBridgeType.Speaker in the core
        private const int Playing = 1;
        private const int SpeakerBridgeType = 1;

        private readonly object _target;
        private readonly Type _interface;

        private MediaPlayerProxy(IControl control, Type iface)
        {
            _target = control;
            _interface = iface;
            Control = control;
        }

        public IControl Control { get; }

        public static MediaPlayerProxy TryCreate(IControl control)
        {
            var iface = control?.GetType().GetInterfaces().FirstOrDefault(i => i.FullName == InterfaceName);
            return iface == null ? null : new MediaPlayerProxy(control, iface);
        }

        public bool IsPlaying => Convert.ToInt32(Get("PlaybackState")) == Playing;
        public int Volume => Convert.ToInt32(Get("Volume"));
        public bool IsMuted => Convert.ToBoolean(Get("IsMuted"));
        public bool IsSpeaker => Convert.ToInt32(Get("BridgeType")) == SpeakerBridgeType;

        public Task PlayAsync() => Call("PlayAsync");
        public Task PauseAsync() => Call("PauseAsync");
        public Task SetVolumeAsync(int volume) => Call("SetVolumeAsync", volume);
        public Task SetMuteAsync(bool muted) => Call("SetMuteAsync", muted);

        public void RegisterValueCallback(Action callback)
        {
            if (_target is IControlValueCallback callbacks)
            {
                callbacks.RegisterValueCallback(callback);
            }
        }

        private object Get(string property) =>
            (_interface.GetProperty(property) ?? throw new MissingMemberException(InterfaceName, property)).GetValue(_target);

        private Task Call(string method, params object[] args)
        {
            var info = _interface.GetMethods().FirstOrDefault(m => m.Name == method && m.GetParameters().Length == args.Length + 1)
                       ?? throw new MissingMethodException(InterfaceName, method);
            try
            {
                return (Task)info.Invoke(_target, args.Append(CancellationToken.None).ToArray());
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                return Task.FromException(e.InnerException);
            }
        }
    }
}
