using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Camera;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Controllers
{
    /// <summary>
    /// Live view and snapshots of the cameras of the camera driver, relayed from go2rtc. The browser never talks to go2rtc:
    /// the camera passwords stay on the server and the visu permissions (user groups) apply.
    /// The WebSocket carries the go2rtc protocol ({"type":"mse",...} + fMP4 fragments); browsers cannot set headers on
    /// WebSockets, so it takes the token as access_token query parameter (like SignalR).
    /// </summary>
    [Route("webapi/camera")]
    [Authorize(Policy = Role.ViewerRole)]
    public class CameraController : BaseController
    {
        public const string CameraTemplateKey = "camera";

        private readonly INodeInstanceCache _nodes;
        private readonly Go2RtcProxy _go2Rtc;
        private readonly ILogger<CameraController> _logger;

        public CameraController(AutomaticaContext dbContext, INodeInstanceCache nodes, Go2RtcProxy go2Rtc, ILogger<CameraController> logger) : base(dbContext)
        {
            _nodes = nodes;
            _go2Rtc = go2Rtc;
            _logger = logger;
        }

        /// <summary>
        /// The live view WebSocket (webapi/camera/{id}/ws): the JWT bearer handler takes the token from the access_token
        /// query parameter for it, browsers cannot send headers on WebSockets
        /// </summary>
        public static bool IsCameraWebSocket(HttpRequest request)
        {
            return request.Path.StartsWithSegments("/webapi/camera") &&
                   string.Equals(request.Headers.Upgrade.ToString(), "websocket", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The camera node of <paramref name="id"/>: the node itself, or the camera above it when a data point of the camera
        /// (online, motion) was picked. Null if it does not exist or the user may not see it.
        /// </summary>
        [NonAction]
        public NodeInstance ResolveCamera(Guid id)
        {
            var camera = _nodes.Get(id);
            var current = camera;
            for (var i = 0; i < 3 && current != null; i++)
            {
                if (current.This2NodeTemplateNavigation?.Key == CameraTemplateKey)
                {
                    camera = current;
                    break;
                }
                current = current.This2ParentNodeInstance.HasValue ? _nodes.Get(current.This2ParentNodeInstance.Value) : null;
            }

            if (camera == null || camera.IsDeleted || !CanAccess(camera.This2UserGroup))
            {
                return null;
            }
            return camera;
        }

        /// <summary>
        /// main → the main stream; anything else → the sub stream if the camera has one, else the main stream
        /// </summary>
        [NonAction]
        public static string PickStream(IReadOnlySet<string> streams, Guid cameraId, string quality)
        {
            var main = Go2RtcProxy.MainStream(cameraId);
            var sub = Go2RtcProxy.SubStream(cameraId);
            if (!string.Equals(quality, "main", StringComparison.OrdinalIgnoreCase) && streams.Contains(sub))
            {
                return sub;
            }
            if (streams.Contains(main))
            {
                return main;
            }
            return streams.Contains(sub) ? sub : null;
        }

        /// <summary>
        /// Which streams go2rtc has for the camera
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Info(Guid id, CancellationToken token)
        {
            var camera = ResolveCamera(id);
            if (camera == null)
            {
                return NotFound();
            }

            var streams = await TryGetStreamsAsync(token);
            if (streams == null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "go2rtc is not reachable" });
            }

            return Ok(new
            {
                id = camera.ObjId,
                name = camera.Name,
                main = streams.Contains(Go2RtcProxy.MainStream(camera.ObjId)),
                sub = streams.Contains(Go2RtcProxy.SubStream(camera.ObjId)),
                snapshot = streams.Contains(Go2RtcProxy.SnapshotStream(camera.ObjId))
            });
        }

        /// <summary>
        /// A JPEG: the camera snapshot if it has a snapshot URL, else a frame of the sub / main stream
        /// </summary>
        [HttpGet("{id:guid}/snapshot")]
        public async Task<IActionResult> Snapshot(Guid id, [FromQuery] string stream, CancellationToken token)
        {
            var camera = ResolveCamera(id);
            if (camera == null)
            {
                return NotFound();
            }

            var streams = await TryGetStreamsAsync(token);
            if (streams == null)
            {
                return StatusCode(StatusCodes.Status502BadGateway);
            }

            var candidates = new List<string>();
            if (streams.Contains(Go2RtcProxy.SnapshotStream(camera.ObjId)))
            {
                candidates.Add(Go2RtcProxy.SnapshotStream(camera.ObjId));
            }
            var picked = PickStream(streams, camera.ObjId, stream);
            if (picked != null)
            {
                candidates.Add(picked);
            }
            if (candidates.Count == 0)
            {
                return NotFound();
            }

            foreach (var candidate in candidates)
            {
                try
                {
                    var frame = await _go2Rtc.GetFrameAsync(candidate, token);
                    if (frame != null)
                    {
                        Response.Headers.CacheControl = "no-store";
                        return File(frame, "image/jpeg");
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested)
                {
                    _logger.LogDebug($"Snapshot of {candidate} failed: {ex.Message}");
                }
            }
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        /// <summary>
        /// The live stream: relays the go2rtc WebSocket of the camera (?stream=main for the full resolution)
        /// </summary>
        [HttpGet("{id:guid}/ws")]
        public async Task Live(Guid id, [FromQuery] string stream)
        {
            var token = HttpContext.RequestAborted;
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var camera = ResolveCamera(id);
            if (camera == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var streams = await TryGetStreamsAsync(token);
            if (streams == null)
            {
                Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            var name = PickStream(streams, camera.ObjId, stream);
            if (name == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            WebSocket upstream;
            try
            {
                upstream = await _go2Rtc.ConnectAsync(name, token);
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or HttpRequestException && !token.IsCancellationRequested)
            {
                _logger.LogWarning($"Camera {camera.Name}: go2rtc WebSocket failed: {ex.Message}");
                Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            using (upstream)
            {
                using var client = await HttpContext.WebSockets.AcceptWebSocketAsync();
                // the token is only checked on connect: end the stream when it expires, the client reconnects with a new one
                using var expiry = CancellationTokenSource.CreateLinkedTokenSource(token);
                var lifetime = TokenLifetime();
                if (lifetime.HasValue)
                {
                    expiry.CancelAfter(lifetime.Value);
                }
                await WebSocketRelay.RunAsync(client, upstream, expiry.Token);
            }
        }

        private TimeSpan? TokenLifetime()
        {
            var exp = User?.FindFirst("exp")?.Value;
            if (exp == null || !long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            {
                return null;
            }
            var left = DateTimeOffset.FromUnixTimeSeconds(seconds) - DateTimeOffset.UtcNow;
            return left > TimeSpan.Zero ? left : TimeSpan.FromMilliseconds(1);
        }

        private async Task<IReadOnlySet<string>> TryGetStreamsAsync(CancellationToken token)
        {
            try
            {
                return await _go2Rtc.GetStreamNamesAsync(token);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException && !token.IsCancellationRequested)
            {
                _logger.LogWarning($"go2rtc at {_go2Rtc.Settings.BaseUri} is not reachable: {ex.Message}");
                return null;
            }
        }
    }
}
