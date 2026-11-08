using System.Text;

namespace P3.Driver.EBus
{
    /// <summary>
    /// Block 1 of the burner/heat generator operational data (05 03), broadcast by the heat generator
    /// </summary>
    /// <param name="Running">status bit 3, "flame": the heat generator produces heat</param>
    /// <param name="CirculationPump">status bit 6, the heat generator's circulation pump runs</param>
    public sealed record HeatGeneratorStatus(byte StatusBits, bool Running, bool CirculationPump, double? FlowTemperature, double? ReturnTemperature,
        double? HotWaterTemperature, double? OutsideTemperature);

    public static class EBusBroadcasts
    {
        public const ushort OperationalData = 0x0503;
        public const ushort WolfErrorMessage = 0xFE01;

        public static HeatGeneratorStatus TryDecodeHeatGeneratorStatus(EBusTelegram telegram)
        {
            var d = telegram.Data;
            if (telegram.Command != OperationalData || d.Length != 8 || d[0] != 0x01)
            {
                return null;
            }

            return new HeatGeneratorStatus(d[2], (d[2] & 0x08) != 0, (d[2] & 0x40) != 0,
                d[4] == 0xFF ? null : d[4] / 2.0,
                d[5] == 0xFF ? null : d[5],
                d[6] == 0xFF ? null : d[6],
                d[7] == 0x80 ? null : (sbyte)d[7]);
        }

        /// <summary>
        /// Wolf heat generators broadcast their fault code as text every few minutes, e.g. "E000 06:29" (code 0 = no fault)
        /// </summary>
        public static int? TryDecodeWolfFaultCode(EBusTelegram telegram)
        {
            if (telegram.Command != WolfErrorMessage || telegram.Data.Length < 4)
            {
                return null;
            }

            var text = Encoding.ASCII.GetString(telegram.Data);
            var end = text.IndexOf(' ');
            var code = end > 1 ? text[1..end] : text[1..];
            return text[0] == 'E' && int.TryParse(code, out var value) ? value : null;
        }
    }
}
