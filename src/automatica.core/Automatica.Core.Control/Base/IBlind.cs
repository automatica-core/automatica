namespace Automatica.Core.Control.Base
{
    public interface IBlind
    {
        Task MoveUpAsync(CancellationToken token = default);
        Task MoveDownAsync(CancellationToken token = default);
        Task MoveRelativeAsync(int pos, CancellationToken token = default);
    }
}
