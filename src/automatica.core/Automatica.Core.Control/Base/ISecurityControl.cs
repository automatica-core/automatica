namespace Automatica.Core.Control.Base
{
    /// <summary>
    /// Who sends a command to a <see cref="ISecurityControl"/>. The web api fills it from the signed-in user, after the
    /// role check, so the control can log who armed, disarmed or acknowledged. It never carries a password or code.
    /// </summary>
    public sealed class SecurityCommandContext
    {
        public SecurityCommandContext(string userName, bool isAdmin, DateTimeOffset timestamp)
        {
            UserName = userName;
            IsAdmin = isAdmin;
            Timestamp = timestamp;
        }

        public string UserName { get; }
        /// <summary>
        /// Administrators may also run the management commands (e.g. the credentials of an access controller)
        /// </summary>
        public bool IsAdmin { get; }
        public DateTimeOffset Timestamp { get; }
    }

    public enum SecurityCommandResult
    {
        Ok = 0,
        /// <summary>
        /// The command is not known to this control
        /// </summary>
        UnknownCommand = 1,
        /// <summary>
        /// The command is known but not possible right now (e.g. arm while the alarm is already armed)
        /// </summary>
        NotPossible = 2,
        /// <summary>
        /// The control refused the command for this user
        /// </summary>
        Denied = 3
    }

    /// <summary>
    /// A security function (burglar, fire/water or emergency alarm, alarm chain, access controller...) that the visualization
    /// shows and commands through the authorised security api instead of writing logic inputs. Implemented by logics.
    /// </summary>
    public interface ISecurityControl : IControl
    {
        /// <summary>
        /// The kind of the control (e.g. "burglar", "burglar-central", "fire", "emergency", "alarm-chain", "access"),
        /// the visualization picks its layout with it
        /// </summary>
        string SecurityKind { get; }

        /// <summary>
        /// The commands the control knows (e.g. "arm", "arm-delayed", "disarm", "acknowledge")
        /// </summary>
        IReadOnlyCollection<string> SecurityCommands { get; }

        /// <summary>
        /// The state for the visualization. Must never contain codes, tags or other secrets.
        /// </summary>
        /// <param name="includeAdminData">True for administrators: the management data too (e.g. the users of an access controller)</param>
        object GetSecurityState(bool includeAdminData);

        /// <param name="command">One of <see cref="SecurityCommands"/></param>
        /// <param name="arguments">Optional arguments of the command (e.g. the user of an access controller). May hold codes:
        /// neither the caller nor the control logs them.</param>
        /// <param name="context">The signed-in user</param>
        /// <param name="token">Cancellation</param>
        Task<SecurityCommandResult> ExecuteSecurityCommandAsync(string command, IReadOnlyDictionary<string, string>? arguments, SecurityCommandContext context, CancellationToken token = default);
    }
}
