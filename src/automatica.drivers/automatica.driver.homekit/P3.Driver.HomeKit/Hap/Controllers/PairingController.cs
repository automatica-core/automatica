using System.Linq;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.TlvData;

namespace P3.Driver.HomeKit.Hap.Controllers
{

    internal sealed class PairingReturn {
        public string ContentType { get; set; }
        public Tlv TlvData { get; set; }

        /// <summary>
        /// The stored (only) controller was removed, the caller must clear the pairing and close the controller's sessions after the response.
        /// </summary>
        public bool PairingRemoved { get; set; }
    }

    /// <summary>
    /// /pairings endpoint. Only a single controller is supported: the one stored by pair-setup, which is an admin.
    /// </summary>
    internal class PairingController : BaseController
    {
        private const int MethodAddPairing = 3;
        private const int MethodRemovePairing = 4;
        private const int MethodListPairings = 5;
        private const int PermissionAdmin = 0x01;

        private readonly ILogger _logger;

        public PairingController(ILogger logger) : base(logger)
        {
            _logger = logger;
        }

        public PairingReturn Post(Tlv parts, HapSession session)
        {
            var state = parts.GetTypeAsInt(Constants.State);
            var method = parts.GetTypeAsInt(Constants.Method);

            var responseTlv = new Tlv();
            var ret = new PairingReturn
            {
                ContentType = "application/pairing+tlv8",
                TlvData = responseTlv
            };
            responseTlv.AddType(Constants.State, 2);

            if (state != 1)
            {
                _logger.LogWarning($"Returning error busy...");
                responseTlv.AddType(Constants.Error, ErrorCodes.Busy);
                return ret;
            }

            // only a verified session belongs to the stored controller, which is the single (admin) controller
            var controllerId = session?.ClientIdentifier;
            var controllerLtpk = HapControllerServer.HapControllerLtpk;
            if (session == null || !session.IsVerified || controllerId == null || string.IsNullOrEmpty(controllerLtpk))
            {
                _logger.LogWarning($"Pairings request from a non admin controller");
                responseTlv.AddType(Constants.Error, ErrorCodes.Authentication);
                return ret;
            }

            var identifier = parts.HasType(Constants.Identifier) ? parts.GetType(Constants.Identifier) : null;

            if (method == MethodAddPairing)
            {
                _logger.LogDebug("* Add Pairing");

                if (identifier == null || !identifier.SequenceEqual(controllerId))
                {
                    // multiple controllers are not supported
                    responseTlv.AddType(Constants.Error, ErrorCodes.Unavailable);
                }
            }
            else if (method == MethodRemovePairing)
            {
                _logger.LogDebug("* Remove Pairing");

                // removing an unknown pairing is a success as well (idempotent)
                ret.PairingRemoved = identifier != null && identifier.SequenceEqual(controllerId);
            }
            else if (method == MethodListPairings)
            {
                _logger.LogDebug("* List Pairings");

                responseTlv.AddType(Constants.Identifier, controllerId);
                responseTlv.AddType(Constants.PublicKey, StringToByteArray(controllerLtpk));
                responseTlv.AddType(Constants.Permissions, PermissionAdmin);
            }
            else
            {
                _logger.LogWarning($"Unknown pairings method {method}");
                responseTlv.AddType(Constants.Error, ErrorCodes.Unavailable);
            }

            return ret;
        }

    }
}
