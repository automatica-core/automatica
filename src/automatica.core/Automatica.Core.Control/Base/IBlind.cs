namespace Automatica.Core.Control.Base
{
    public interface IBlind
    {
        Task StopAsync(CancellationToken token = default);
        Task MoveUpAsync(CancellationToken token = default);
        Task MoveDownAsync(CancellationToken token = default);
        Task MoveAbsoluteAsync(int pos, CancellationToken token = default);
    }
}
