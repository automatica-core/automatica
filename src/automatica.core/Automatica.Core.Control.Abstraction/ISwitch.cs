﻿namespace Automatica.Core.Control.Abstraction
{
    public enum SwitchState
    {
        Unknown = -1,
        Off = 0,
        On = 1,
    }
    
    public interface ISwitch : IControl
    {
        public SwitchState State { get; }
        public Task<bool> SwitchAsync(bool state, CancellationToken cancellationToken = default);
        public Task<bool> SwitchAsync(SwitchState state, CancellationToken cancellationToken = default);


        public Task<bool> SwitchOnAsync(CancellationToken cancellationToken = default);
        public Task<bool> SwitchOffAsync(CancellationToken cancellationToken = default);
    }
}
