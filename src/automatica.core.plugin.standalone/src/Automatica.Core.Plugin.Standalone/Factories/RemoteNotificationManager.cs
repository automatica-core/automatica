using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Notification;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    /// <summary>
    /// Notifications are stored by the master, which the standalone plugin has no channel for yet - they are logged instead.
    /// </summary>
    internal class RemoteNotificationManager : INotificationManager
    {
        private readonly ILogger _logger;

        public RemoteNotificationManager(ILogger logger)
        {
            _logger = logger;
        }

        public Task CreateNotification(NodeInstance nodeInstance, string body, string subject, NotificationSeverity severity, CancellationToken token = default)
        {
            _logger.LogInformation($"Notification ({severity}) for {nodeInstance?.Name}: {subject} - {body}");
            return Task.CompletedTask;
        }

        public Task CreateNotification(RuleInstance ruleInstance, string body, string subject, NotificationSeverity severity, CancellationToken token = default)
        {
            _logger.LogInformation($"Notification ({severity}) for {ruleInstance?.Name}: {subject} - {body}");
            return Task.CompletedTask;
        }

        public Task DismissNotification(Guid notificationId, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public Task DismissNotification(EF.Models.Notification notification, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }
    }
}
