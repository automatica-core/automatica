using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.TlvData;

namespace P3.Driver.HomeKit.Hap.Controllers
{

    internal sealed class PairingReturn {
        public string ContentType { get; set; }
        public byte[] Data { get; set; }

        /// <summary>
        /// Identifiers whose pairing was removed by this request; their connections must be closed.
        /// </summary>
        public IList<string> RemovedControllers { get; set; } = new List<string>();
    }

    internal class PairingController : BaseController
    {
        internal const int MethodAddPairing = 3;
        internal const int MethodRemovePairing = 4;
        internal const int MethodListPairings = 5;

        private const byte PermissionAdmin = 0x01;

        private readonly ILogger _logger;

        public PairingController(ILogger logger) : base(logger)
        {
            _logger = logger;
        }

        private PairingReturn Result(Tlv tlv)
        {
            return new PairingReturn
            {
                ContentType = "application/pairing+tlv8",
                Data = TlvParser.Serialize(tlv)
            };
        }

        private PairingReturn Error(ErrorCodes error)
        {
            var responseTlv = new Tlv();
            responseTlv.AddType(Constants.State, 2);
            responseTlv.AddType(Constants.Error, error);
            return Result(responseTlv);
        }

        public PairingReturn Post(Tlv parts, HapSession session, HapPairingStore store)
        {
            var state = parts.GetTypeAsInt(Constants.State);
            var method = parts.GetTypeAsInt(Constants.Method);

            if (state != 1)
            {
                _logger.LogWarning($"Invalid pairings state {state}");
                return Error(ErrorCodes.Unknown);
            }

            // HAP 5.10 - 5.12: only verified admin controllers may manage pairings
            if (session == null || !session.IsVerified || !session.IsAdmin)
            {
                _logger.LogWarning("Pairings request from a non admin controller rejected");
                return Error(ErrorCodes.Authentication);
            }

            switch (method)
            {
                case MethodAddPairing:
                {
                    _logger.LogDebug("* Add Pairing");

                    if (!parts.HasType(Constants.Identifier) || !parts.HasType(Constants.PublicKey) ||
                        !parts.HasType(Constants.Permissions))
                    {
                        return Error(ErrorCodes.Unknown);
                    }

                    var identifier = Encoding.UTF8.GetString(parts.GetType(Constants.Identifier));
                    var publicKey = parts.GetType(Constants.PublicKey);
                    var permissions = parts.GetType(Constants.Permissions);

                    if (publicKey.Length != 32 || permissions.Length != 1)
                    {
                        return Error(ErrorCodes.Unknown);
                    }

                    var error = store.AddPairing(identifier, ByteArrayToString(publicKey),
                        (permissions[0] & PermissionAdmin) == PermissionAdmin);

                    if (error.HasValue)
                    {
                        return Error(error.Value);
                    }

                    var responseTlv = new Tlv();
                    responseTlv.AddType(Constants.State, 2);
                    return Result(responseTlv);
                }
                case MethodRemovePairing:
                {
                    _logger.LogDebug("* Remove Pairing");

                    if (!parts.HasType(Constants.Identifier))
                    {
                        return Error(ErrorCodes.Unknown);
                    }

                    var identifier = Encoding.UTF8.GetString(parts.GetType(Constants.Identifier));
                    var removed = store.RemovePairing(identifier);

                    var responseTlv = new Tlv();
                    responseTlv.AddType(Constants.State, 2);
                    var result = Result(responseTlv);
                    result.RemovedControllers = removed;
                    return result;
                }
                case MethodListPairings:
                {
                    _logger.LogDebug("* List Pairings");
                    return new PairingReturn
                    {
                        ContentType = "application/pairing+tlv8",
                        Data = SerializeList(store.Pairings)
                    };
                }
                default:
                    _logger.LogWarning($"Unknown pairings method {method}");
                    return Error(ErrorCodes.Unknown);
            }
        }

        internal static byte[] SerializeList(IReadOnlyList<HapPairing> pairings)
        {
            using (var ms = new MemoryStream())
            {
                WriteItem(ms, Constants.State, new byte[] { 2 });

                for (var i = 0; i < pairings.Count; i++)
                {
                    if (i > 0)
                    {
                        WriteItem(ms, Constants.Separator, Array.Empty<byte>());
                    }

                    var pairing = pairings[i];
                    WriteItem(ms, Constants.Identifier, Encoding.UTF8.GetBytes(pairing.Identifier ?? string.Empty));
                    WriteItem(ms, Constants.PublicKey, StringToByteArray(pairing.PublicKey));
                    WriteItem(ms, Constants.Permissions, new[] { pairing.IsAdmin ? PermissionAdmin : (byte)0 });
                }

                return ms.ToArray();
            }
        }

        private static void WriteItem(Stream stream, Constants type, byte[] value)
        {
            if (value.Length == 0)
            {
                stream.WriteByte((byte)type);
                stream.WriteByte(0);
                return;
            }

            foreach (var slice in value.Slices(255))
            {
                stream.WriteByte((byte)type);
                stream.WriteByte((byte)slice.Length);
                stream.Write(slice, 0, slice.Length);
            }
        }
    }
}
