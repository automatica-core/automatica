using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.IO
{
    /// <summary>
    /// Interface between UI and driver
    /// </summary>
    public interface INotifyDriver
    {
        /// <summary>
        /// Notification when a <see cref="NodeInstance"/> is saved
        /// </summary>
        /// <param name="node">Saved <see cref="NodeInstance"/></param>
        /// <returns><see cref="Task"/></returns>
        Task NotifyUpdate(NodeInstance node);

        /// <summary>
        /// Notification when a <see cref="NodeInstance"/> is saved
        /// </summary>
        /// <param name="node">Saved <see cref="NodeInstance"/></param>
        /// <returns><see cref="Task"/></returns>
        Task NotifyAdd(NodeInstance node);

        /// <summary>
        /// Notification when a <see cref="NodeInstance"/> is deleted
        /// </summary>
        /// <param name="node">Deleted <see cref="NodeInstance"/></param>
        /// <returns><see cref="Task"/></returns>
        Task NotifyDeleted(NodeInstance node);

        /// <summary>
        /// Notification when a <see cref="NodeInstance"/> should read the value
        /// </summary>
        /// <param name="node"><see cref="NodeInstance"/> to read</param>
        /// <returns><see cref="Task"/> true if successful</returns>
        Task<bool> Read(NodeInstance node);

        /// <summary>
        /// Notification when the <see cref="NodeInstance"/> should scan the bus for new <see cref="NodeInstance"/>s.
        /// </summary>
        /// <param name="node"><see cref="NodeInstance"/> to scan</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> ScanBus(NodeInstance node);

        /// <summary>
        /// Notification when the <see cref="NodeInstance"/> should import a file.
        /// </summary>
        /// <param name="node"><see cref="NodeInstance"/> to import</param>
        /// <param name="fileName">Absolute filepath</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> Import(NodeInstance node, string fileName);

        /// <summary>
        /// Notification on a custom action
        /// </summary>
        /// <param name="node"><see cref="NodeInstance"/> to run the custom action</param>
        /// <param name="actionName">The action name</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> CustomAction(NodeInstance node, string actionName);

        /// <summary>
        /// Action to enable the learn mode on the given node instance
        /// </summary>
        /// <param name="node"><see cref="NodeInstance"/> to enable the learn mode</param>
        /// <returns>True if successful</returns>
        Task<bool> EnableLearnMode(NodeInstance node);

        /// <summary>
        /// Action to disable the learn mode on the given node instance
        /// </summary>
        /// <param name="node"><see cref="NodeInstance"/> to enable the learn mode</param>
        /// <returns>True if successful</returns>
        Task<bool> DisableLearnMode(NodeInstance node);
    }
}
