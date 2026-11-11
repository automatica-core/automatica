using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Data
{
    public class EnOceanGenericData : EnOceanDataNode
    {
        public EnOceanGenericData(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        /// <summary>
        /// The two-state enum mapping (first range = false, second range = true) only applies to fields whose
        /// enum really has two states. Nodes created by older driver versions carry the enum properties on
        /// multi-state fields as well (e.g. A5-10 fan speed), the template decides.
        /// </summary>
        private bool UseBoolEnum => !Template.HasValue || EnOceanEepMetadata.IsBoolEnum(Template.Value);

        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            var data = GetValueGeneric(telegram);

            DriverContext.Logger.LogDebug($"Parsed data {data}");

            if (data != null)
            {
                if (data is int dataInt && UseBoolEnum && EnumFirstMax.HasValue && EnumFirstMin.HasValue && EnumSecondMax.HasValue && EnumSecondMin.HasValue)
                {
                    if (dataInt >= EnumFirstMin && dataInt <= EnumFirstMax)
                    {
                        DispatchRead(false);
                    }
                    else if (dataInt >= EnumSecondMin && dataInt <= EnumSecondMax)
                    {
                        DispatchRead(true);
                    }
                    else
                    {
                        DispatchRead(data);
                    }
                }
                else
                {
                    DispatchRead(data);
                }
            }
        }
    }
}
