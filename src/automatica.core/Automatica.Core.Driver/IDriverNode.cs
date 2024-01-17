using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Base interface for a <see cref="IDriverNode"/>
    /// </summary>
    public interface IDriverNode : IDispatchable
    {

        /// <summary>
        /// The context for the given instance
        /// </summary>
        IDriverContext DriverContext { get; }
        /// <summary>
        /// List of children
        /// </summary>
        IList<IDriverNode> Children { get; }

        /// <summary>
        /// Configure method of the node. Will only by called once at startup
        /// </summary>
        /// <returns>
        /// true if successful
        /// if false will be returned, the childs will be ignored
        /// </returns>
        Task<bool> Configure(CancellationToken token = default);

        /// <summary>
        /// Init method of the node. Will be called after <see cref="IDriverNode.Configure"/>
        /// </summary>
        /// <returns>
        /// true if successful
        /// if false will be returned, the childs will be ignored
        /// </returns>
        Task<bool> Init(CancellationToken token = default);

        /// <summary>
        /// Parent of current node
        /// </summary>
        IDriverNode Parent { get; set; }

        /// <summary>
        /// Entry method of the driver
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> Start(CancellationToken token = default);


        /// <summary>
        /// Notification when all drivers/logics are started. This is the point when the dispatcher is fully initialized
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> Started(CancellationToken token = default);

        /// <summary>
        /// Will be called on shutdown
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> Stop(CancellationToken token = default);


        /// <summary>
        /// Notification when all drivers/logics are stopped
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> Stopped(CancellationToken token = default);

        /// <summary>
        /// Will be called to scan the driver for <see cref="NodeInstance"/>
        /// </summary>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> Scan(CancellationToken token = default);

        /// <summary>
        /// Will be called to import data from a file
        /// </summary>
        /// <param name="fileName">The filename to the uploaded file</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> Import(string fileName, CancellationToken token = default);


        /// <summary>
        /// Will be called to import data from a file
        /// </summary>
        /// <param name="config">The config to the uploaded file</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> Import(ImportConfig config, CancellationToken token = default);

        /// <summary>
        /// Will be called if a custom action is called
        /// </summary>
        /// <param name="actionName">The name of the action</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> CustomAction(string actionName, CancellationToken token = default);


        /// <summary>
        /// Will be called from the <see cref="IDispatcher"/> when a value should be written to the driver
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="value">The value</param>
        /// <returns><see cref="Task"/></returns>
        Task WriteValue(IDispatchable source, DispatchValue value, CancellationToken token = default);

        /// <summary>
        /// Indicates the driver to read a value
        /// </summary>
        /// <returns></returns>
        Task<bool> Read(CancellationToken token = default);

        /// <summary>
        /// Will be called if the configuration will be saved
        /// </summary>
        /// <param name="instance">The saved <see cref="NodeInstance"/></param>
        /// <returns>async <see cref="Task"/></returns>
        Task OnSave(NodeInstance instance, CancellationToken token = default);

        /// <summary>
        /// Will be called if a <see cref="NodeInstance"/>will be deleted
        /// </summary>
        /// <param name="instance">The deleted <see cref="NodeInstance"/></param>
        /// <returns>async <see cref="Task"/></returns>
        Task OnDelete(NodeInstance instance, CancellationToken token = default);

        /// <summary>
        /// Will be called when a ReInit is called 
        /// The <see cref="IDriverNode"/> <see cref="Stop"/> and <see cref="Start"/> method will be called
        /// </summary>
        /// <returns></returns>
        Task OnReInit(CancellationToken token = default);


        int ChildrensCreated { get; }
        string Error { get; }

        /// <summary>
        /// Returns the state of the NodeInstance
        /// </summary>
        NodeInstanceState State { get; }

        /// <summary>
        /// Will be called if the learn mode should be activated
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> EnableLearnMode(CancellationToken token = default);

        /// <summary>
        /// Will be called if the learn mode should be deactivated
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> DisableLearnMode(CancellationToken token = default);
    }
}
