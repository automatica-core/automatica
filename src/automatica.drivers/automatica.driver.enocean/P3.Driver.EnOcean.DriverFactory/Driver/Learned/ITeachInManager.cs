using System.Threading.Tasks;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.DriverFactory.Driver.Learned
{
    public interface ITeachInManager
    {
        /// <summary>
        /// Registers a device that has a node, learn mode reports it as already known.
        /// </summary>
        void SetTeachedIn(string serialnumber, string name);

        /// <summary>
        /// Sends a telegram over the USB stick (actuator commands, teach-in).
        /// </summary>
        /// <returns>false if the stick is not open, its id base is unknown or it rejected the telegram</returns>
        Task<bool> Send(RadioErp1Packet telegram);

        /// <summary>
        /// Registers the command case of a bidirectional device (e.g. A5-20-01 controller to actuator), its status case
        /// answers every status telegram with it.
        /// </summary>
        void SetCommandNode(string serialnumber, EnOceanTypeNode node);

        /// <returns>null if the device has no command case node</returns>
        EnOceanTypeNode GetCommandNode(string serialnumber);

        /// <returns>true once per device within a few seconds: the first of several case nodes of a device answers its teach-in query</returns>
        bool ClaimTeachInResponse(string serialnumber);
    }
}
