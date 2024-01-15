namespace Automatica.Core.Control.Base
{
    public interface IBlind : IControl, IControlValueCallback
    {
        bool IsMoving { get; }
        int Direction { get; }
        int Position { get; }

        Task StopAsync(CancellationToken token = default);
        Task MoveUpAsync(CancellationToken token = default);
        Task MoveDownAsync(CancellationToken token = default);
        Task MoveAbsoluteAsync(int pos, CancellationToken token = default);
    }
}
