using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    /// <summary>
    /// Grid charge flag (bit 0) of one time of use slot, or of all slots. Writing changes only bit 0 of the charge
    /// registers; the generator charge bit and every other bit stay as the inverter holds them.
    /// </summary>
    internal class SolarmanGridChargeAttribute : SolarmanAttrribute
    {
        /// <summary>
        /// Index of the slot in the time of use charge registers (0 = slot 1), null for all slots.
        /// </summary>
        public int? SlotIndex { get; }

        public SolarmanGridChargeAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent, int? slotIndex) : base(driverContext, parent)
        {
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// One register for a slot; for all slots, true while at least one slot charges from the grid.
        /// </summary>
        public override Task<object> ConvertValue(ushort[] data)
        {
            object value = SlotIndex.HasValue
                ? SolarmanRegisterCodec.IsGridChargeEnabled(data[0])
                : SolarmanRegisterCodec.IsGridChargeEnabledInAnySlot(data);
            return Task.FromResult(value);
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            bool enabled;
            try
            {
                enabled = SolarmanRegisterCodec.ToBool(value);
            }
            catch (Exception e) when (e is FormatException or ArgumentNullException or InvalidCastException or OverflowException)
            {
                // an invalid value stays invalid, a retry cannot help
                DriverContext.Logger.LogError(e, $"Could not convert value ({value}) for grid charge {Describe()}");
                return;
            }

            DriverContext.Logger.LogInformation($"WRITE grid charge {(enabled ? "on" : "off")} for {Describe()}");

            bool state;
            try
            {
                state = await Group.WriteGridCharge(SlotIndex, enabled, this, token);
            }
            catch (InvalidOperationException e)
            {
                // the write would touch a register or bit outside the grid charge flags, nothing was written
                DriverContext.Logger.LogError(e, $"Refused to write grid charge for {Describe()}");
                return;
            }

            await writeContext.DispatchValue(state, token);
        }

        private string Describe()
        {
            return SlotIndex.HasValue ? $"time of use slot {SlotIndex.Value + 1}" : "all time of use slots";
        }
    }
}
