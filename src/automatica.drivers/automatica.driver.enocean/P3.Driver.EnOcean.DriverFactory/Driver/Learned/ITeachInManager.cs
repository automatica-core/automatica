using System.Threading.Tasks;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Learned
{
    public interface ITeachInManager
    {
        void SetTeachedIn(string serialnumber);

        /// <summary>
        /// Sends a telegram over the USB stick (actuator commands, teach-in).
        /// </summary>
        /// <returns>false if the stick is not open, its id base is unknown or it rejected the telegram</returns>
        Task<bool> Send(RadioErp1Packet telegram);
    }
}
