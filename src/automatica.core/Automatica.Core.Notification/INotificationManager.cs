using Automatica.Core.EF.Models;

namespace Automatica.Core.Notification
{
    public interface INotificationManager
    {
        Task CreateNotification(NodeInstance nodeInstance, string body, string subject,
            NotificationSeverity severity, CancellationToken token = default);

        Task CreateNotification(RuleInstance ruleInstance, string body, string subject,
            NotificationSeverity severity, CancellationToken token = default);

        /// <summary>
        /// Notification raised by the automation <paramref name="automationId"/> (notify action, automation switched off, ...)
        /// </summary>
        Task CreateAutomationNotification(Guid automationId, string body, string subject,
            NotificationSeverity severity, CancellationToken token = default);

        Task DismissNotification(Guid notificationId, CancellationToken token = default);
        Task DismissNotification(EF.Models.Notification notification, CancellationToken token = default);
    }
}
