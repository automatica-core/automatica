using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// The interface for providing a driver factory
    /// </summary>
    public interface IDriverFactory : IFactory<INodeTemplateFactory>
    {
        /// <summary>
        /// The driverName is mainly used for logging
        /// </summary>
        string DriverName { get; }

        /// <summary>
        /// Driver Guid needs to be unique across the system
        /// </summary>
        Guid DriverGuid { get; }

        /// <summary>
        /// Driver Version indicates if something changed in the <see cref="NodeTemplate"/> definition. Increment to define that the <see cref="NodeTemplate"/> will be updated
        /// </summary>
        Version DriverVersion { get; }

        /// <summary>
        /// Indicates that the factory is in development mode and the <see cref="InitNodeTemplates(INodeTemplateFactory)"/> method will be called on every start
        /// </summary>
        bool InDevelopmentMode { get; }

        /// <summary>
        /// Init method for the factory
        /// </summary>
        /// <param name="factory"><see cref="INodeTemplateFactory"/> for adding/updating NodeTemplates</param>
        void InitNodeTemplates(INodeTemplateFactory factory);

        /// <summary>
        /// Returns a new instance of the <see cref="IDriver"/>
        /// </summary>
        /// <param name="config">Context paramters</param>
        /// <returns></returns>
        IDriver CreateDriver(IDriverContext config);


        /// <summary>
        /// Scan callback. Will be called when the UI notifies the factory to scan the driver
        /// </summary>
        /// <param name="instance"><see cref="NodeInstance"/> instance to scan</param>
        void Scan(NodeInstance instance);


        /// <summary>
        /// Deterimnes where to get the image
        /// </summary>
        string ImageSource { get; }

        /// <summary>
        /// Deterimnes the image name
        /// </summary>
        string ImageName { get; }


        /// <summary>
        /// Deterimnes the image tag - default "latest"
        /// </summary>
        string Tag { get; }
    }
}
