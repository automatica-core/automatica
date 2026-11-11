using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Data
{
    /// <summary>
    /// Writing true sends the teach-in telegram of the profile, so an actuator in learn mode learns the sender id
    /// Automatica uses for this device.
    /// </summary>
    public class EnOceanTeachIn : EnOceanDataNode
    {
        public EnOceanTeachIn(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        protected override bool HasField => false;

        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            // not a field of a received telegram
        }

        internal override async Task<bool> SendValue(object value, IWriteContext writeContext)
        {
            bool send;
            try
            {
                send = Convert.ToBoolean(value);
            }
            catch (Exception e) when (e is FormatException || e is InvalidCastException)
            {
                send = false;
            }

            if (!send)
            {
                return false;
            }

            if (Device == null || !await Device.SendTeachIn())
            {
                DriverContext.Logger.LogWarning($"{Name}: sending the teach-in telegram failed");
                return false;
            }

            if (writeContext != null)
            {
                await writeContext.DispatchValue(true);
            }
            return true;
        }
    }
}
