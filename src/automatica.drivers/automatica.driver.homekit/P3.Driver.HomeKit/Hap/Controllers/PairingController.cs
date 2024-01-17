using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.TlvData;

namespace P3.Driver.HomeKit.Hap.Controllers
{

    internal sealed class PairingReturn {
        public string ContentType { get; set; }
        public Tlv TlvData { get; set; }
    }

    internal class PairingController : BaseController
    {
        private readonly ILogger _logger;

        public PairingController(ILogger logger) : base(logger)
        {
            _logger = logger;
        }

        public PairingReturn Post(Tlv parts)
        {
            var state = parts.GetTypeAsInt(Constants.State);
            var method = parts.GetTypeAsInt(Constants.Method);

            var responseTlv = new Tlv();


            if (state == 1)
            {
                if (method == 3) // Add Pairing
                {
                    _logger.LogDebug("* Add Pairing");

                    var identifier = parts.GetType(Constants.Identifier);
                    var publickey = parts.GetType(Constants.PublicKey);
                    var permissions = parts.GetType(Constants.Permissions);
                    responseTlv.AddType(Constants.State, 2);
                }
                else if (method == 4) // Remove Pairing
                {
                    _logger.LogDebug("* Remove Pairing");
                    responseTlv.AddType(Constants.State, 2);
                    
                }
                else if (method == 5) // List Pairing
                {
                    _logger.LogDebug("* List Pairings");
                    responseTlv.AddType(Constants.State, 2);
                }
            }
            else
            {
                _logger.LogWarning($"Returning error busy...");
                responseTlv.AddType(Constants.State, 2);
                responseTlv.AddType(Constants.Error, ErrorCodes.Busy);
            }

            return new PairingReturn
            {
                ContentType = "application/pairing+tlv8",
                TlvData = responseTlv
            };
        }

    }
}
