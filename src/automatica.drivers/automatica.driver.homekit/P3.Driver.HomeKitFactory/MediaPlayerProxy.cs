using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;

namespace P3.Driver.HomeKitFactory
{
    /// <summary>
    /// Reaches a core <c>Automatica.Core.Control.Base.IMediaPlayer</c> by name. The plugin builds against the published core
    /// NuGet, which does not have the interface yet; on a core that has it, media player controls are exposed, on older
    /// cores there are none.
    /// </summary>
    internal sealed class MediaPlayerProxy
    {
        internal const string InterfaceName = "Automatica.Core.Control.Base.IMediaPlayer";

        // MediaPlaybackState.Playing in the core
        private const int Playing = 1;

        private readonly Type _interface;

        private MediaPlayerProxy(IControl control, Type iface)
        {
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

        public Task PlayAsync() => Call("PlayAsync");
        public Task PauseAsync() => Call("PauseAsync");
        public Task SetVolumeAsync(int volume) => Call("SetVolumeAsync", volume);
        public Task SetMuteAsync(bool muted) => Call("SetMuteAsync", muted);

        public void RegisterValueCallback(Action callback)
        {
            if (Control is IControlValueCallback callbacks)
            {
                callbacks.RegisterValueCallback(callback);
            }
        }

        private object Get(string property) =>
            (_interface.GetProperty(property) ?? throw new MissingMemberException(InterfaceName, property)).GetValue(Control);

        private Task Call(string method, params object[] args)
        {
            var info = _interface.GetMethods().FirstOrDefault(m => m.Name == method && m.GetParameters().Length == args.Length + 1)
                       ?? throw new MissingMethodException(InterfaceName, method);
            try
            {
                return (Task)info.Invoke(Control, args.Append(CancellationToken.None).ToArray());
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                return Task.FromException(e.InnerException);
            }
        }
    }
}
