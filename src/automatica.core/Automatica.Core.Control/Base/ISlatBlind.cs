namespace Automatica.Core.Control.Base
{
    /// <summary>
    /// A blind with tiltable slats (venetian blind, "Jalousie"). Positions are percent: 0 is open, 100 is closed,
    /// for the blind (top/bottom) and for the slats (horizontal/closed), like KNX DPT 5.001.
    /// </summary>
    public interface ISlatBlind : IBlind
    {
        /// <summary>
        /// False when the slat datapoints of the blind are not connected
        /// </summary>
        bool HasSlats { get; }

        int SlatPosition { get; }

        Task MoveSlatAsync(int pos, CancellationToken token = default);
    }
}
