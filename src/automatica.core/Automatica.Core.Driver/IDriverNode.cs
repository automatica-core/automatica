using System;
using System.Collections.Generic;
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
        /// true if successfull
        /// if false will be returned, the childs will be ignored
        /// </returns>
        bool Configure();

        /// <summary>
        /// Init method of the node. Will be called after <see cref="IDriverNode.Configure"/>
        /// </summary>
        /// <returns>
        /// true if successfull
        /// if false will be returned, the childs will be ignored
        /// </returns>
        bool Init();

        /// <summary>
        /// Parent of current node
        /// </summary>
        IDriverNode Parent { get; set; }

        /// <summary>
        /// Entry method of the driver
        /// </summary>
        /// <returns>True if successfull</returns>
        Task<bool> Start();

        /// <summary>
        /// Will be called on shutdown
        /// </summary>
        /// <returns>True if successfull</returns>
        Task<bool> Stop();

        /// <summary>
        /// Will be called to scan the driver for <see cref="NodeInstance"/>
        /// </summary>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> Scan();

        /// <summary>
        /// Will be called to import data from a file
        /// </summary>
        /// <param name="fileName">The filename to the uploaded file</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> Import(string fileName);

        /// <summary>
        /// Will be called if a custom action is called
        /// </summary>
        /// <param name="actionName">The name of the action</param>
        /// <returns>A list of new <see cref="NodeInstance"/></returns>
        Task<IList<NodeInstance>> CustomAction(string actionName);

        /// <summary>
        /// Will be called from the <see cref="IDispatcher"/> when a value should be written to the driver
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="value">The value</param>
        /// <returns><see cref="Task"/></returns>
        [Obsolete("Use WriteValue with DispatchValue instead")]
        Task WriteValue(IDispatchable source, object value);


        /// <summary>
        /// Will be called from the <see cref="IDispatcher"/> when a value should be written to the driver
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="value">The value</param>
        /// <returns><see cref="Task"/></returns>
        Task WriteValue(IDispatchable source, DispatchValue value);

        /// <summary>
        /// Indicates the driver to read a value
        /// </summary>
        /// <returns></returns>
        Task<bool> Read();

        /// <summary>
        /// Will be called if the configuration will be saved
        /// </summary>
        /// <param name="instance">The saved <see cref="NodeInstance"/></param>
        /// <returns>async <see cref="Task"/></returns>
        Task OnSave(NodeInstance instance);

        /// <summary>
        /// Will be called if a <see cref="NodeInstance"/>will be deleted
        /// </summary>
        /// <param name="instance">The deleted <see cref="NodeInstance"/></param>
        /// <returns>async <see cref="Task"/></returns>
        Task OnDelete(NodeInstance instance);

        /// <summary>
        /// Will be called when a ReInit is called 
        /// The <see cref="IDriverNode"/> <see cref="Stop"/> and <see cref="Start"/> method will be called
        /// </summary>
        /// <returns></returns>
        Task OnReinit();


        int ChildrensCreated { get; }

        /// <summary>
        /// Returns the state of the NodeInstance
        /// </summary>
        NodeInstanceState State { get; }

        /// <summary>
        /// Will be called if the learn mode should be activated
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> EnableLearnMode();

        /// <summary>
        /// Will be called if the learn mode should be deactivated
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> DisableLearnMode();
    }
}
