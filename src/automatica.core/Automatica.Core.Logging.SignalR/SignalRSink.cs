using Automatica.Core.Logging.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Serilog.Core;
using Serilog.Events;

namespace Automatica.Core.Logging.SignalR
{
    public class SignalRSink<THub, T> : ILogEventSink
        where THub : Hub<T> where T : class
    {
        private readonly IHubContext<THub, T> _hubContext;
        private readonly string _facility;
        private readonly IFormatProvider? _formatProvider;
        private readonly string[] _groups;
        private readonly string[] _userIds;
        private readonly string[] _excludedConnectionIds;

        /// <summary>
        /// SignalRSink constructor.
        /// </summary>
        /// <param name="hubContext">The hub where the events are emitted.</param>
        /// <param name="facility">Name of the logfile</param>
        /// <param name="formatProvider">The format provider with which the events are formatted.</param>
        /// <param name="groups">The groups where the events are sent.</param>
        /// <param name="userIds">The users to where the events are sent.</param>
        /// <param name="excludedConnectionIds">The client ids to exclude.</param>
        public SignalRSink(
            IHubContext<THub, T> hubContext,
            string facility,
            IFormatProvider? formatProvider = null,
            string[]? groups = null,
            string[]? userIds = null,
            string[]? excludedConnectionIds = null
        )
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _facility = facility;
            _formatProvider = formatProvider;
            _groups = groups ?? Array.Empty<string>();
            _userIds = userIds ?? Array.Empty<string>();
            _excludedConnectionIds = excludedConnectionIds ?? Array.Empty<string>();
        }

        /// <summary>
        /// Emit a log event to the clients registered to the hub.
        /// </summary>
        /// <param name="logEvent">The event to emit</param>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            var targets = new List<T>();

            if (_groups.Any())
            {
                targets.Add(_hubContext
                    .Clients
                    .Groups(_groups
                        .Except(_excludedConnectionIds)
                        .ToArray()
                    )
                );
            }

            if (_userIds.Any())
            {
                targets.Add(_hubContext
                    .Clients
                    .Users(_userIds
                        .Except(_excludedConnectionIds)
                        .ToArray()
                    )
                );
            }

            if (!_groups.Any() && !_userIds.Any())
            {
                targets.Add(_hubContext
                    .Clients
                    .AllExcept(_excludedConnectionIds)
                );
            }

            foreach (var target in targets)
            {
                ((ISerilogHub)target)
                    .PushEventLog(_facility, logEvent.RenderMessage(_formatProvider));
            }
        }
    }
}
