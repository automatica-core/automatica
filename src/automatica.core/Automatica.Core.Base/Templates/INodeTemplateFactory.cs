using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.Templates
{
 
    public interface INodeTemplateFactory : IPropertyTemplateFactory
    {
        /// <summary>
        /// Gets a <see cref="NodeTemplate"/> by its unique id
        /// </summary>
        /// <param name="id">The unique guid</param>
        /// <returns><see cref="NodeTemplate"/></returns>
        NodeTemplate GetNodeTemplateById(Guid id);

        /// <summary>
        /// Gets a <see cref="NodeTemplate"/> by its key
        /// </summary>
        /// <param name="key">The key used while generating the <see cref="NodeTemplate"/></param>
        /// <returns><see cref="NodeTemplate"/></returns>
        NodeTemplate GetNodeTemplateByKey(string key);

        /// <summary>
        /// Gets all <see cref="NodeTemplate"/> by its id
        /// </summary>
        /// <param name="key">The key used while generating the <see cref="NodeTemplate"/></param>
        /// <returns>List of <see cref="NodeTemplate"/></returns>
        ICollection<NodeTemplate> GetNodeTemplates(params Guid[] key);

        /// <summary>
        /// Creates a <see cref="NodeInstance"/> using the <see cref="NodeTemplate"/>
        /// </summary>
        /// <param name="template"></param>
        /// <returns>The generated <see cref="NodeInstance"/></returns>
        NodeInstance CreateNodeInstance(NodeTemplate template);

        /// <summary>
        /// Creates a <see cref="NodeInstance"/> using the unique id of the <see cref="NodeTemplate"/>
        /// </summary>
        /// <param name="template"></param>
        /// <returns>The generated <see cref="NodeInstance"/></returns>
        NodeInstance CreateNodeInstance(Guid template);

        /// <summary>
        /// Creates a <see cref="NodeInstance"/> using the key of the <see cref="NodeTemplate"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The generated <see cref="NodeInstance"/></returns>
        NodeInstance CreateNodeInstanceByKey(string key);


        /// <summary>
        /// Creates a new interface type
        /// </summary>
        /// <param name="uid">The unique id</param>
        /// <param name="name">The name of the interface type</param>
        /// <param name="description">The description of the interface type</param>
        /// <param name="maxChilds">Defines the max children the interface type can have</param>
        /// <param name="maxInstances">Defines the max instances which can be created of the interface</param>
        /// <param name="isDriverInterface">Defines if the interface is a driver interface or not</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateInterfaceType(Guid uid, string name, string description, int maxChilds, int maxInstances,
            bool isDriverInterface);

        /// <summary>
        /// Creates a new <see cref="NodeTemplate"/>
        /// </summary>
        /// <param name="uid">The unique id</param>
        /// <param name="name">The name used to create the <see cref="NodeInstance.Name"/></param>
        /// <param name="description">The description used to create the <see cref="NodeInstance.Description"/></param>
        /// <param name="key">The key of the <see cref="NodeTemplate"/></param>
        /// <param name="needsInterface">Defines the InterfaceType which is needed to create this <see cref="NodeTemplate"/></param>
        /// <param name="providesInterface">Defines which InterfaceType this <see cref="NodeTemplate"/> provides</param>
        /// <param name="defaultCreated">Defines if the <see cref="NodeTemplate"/> will be created by default when adding the parent node</param>
        /// <param name="isReadable">Defines if the created <see cref="NodeInstance"/> can be read</param>
        /// <param name="isReadableFixed">Defines if the user can change the read flag of the <see cref="NodeInstance"/></param>
        /// <param name="isWriteable">>Defines if the created <see cref="NodeInstance"/> can be written</param>
        /// <param name="isWriteableFixed">Defines if the user can change the write flag of the <see cref="NodeInstance"/></param>
        /// <param name="dataType">Defines the data type which the <see cref="NodeInstance"/> provides. If it is just a container for other nodes set it to <see cref="NodeDataType.NoAttribute"/> </param>
        /// <param name="maxInstances">Defines how many <see cref="NodeInstance"/> can be created from this <see cref="NodeTemplate"/></param>
        /// <param name="isAdapterInterface">defines if the node is just a adapter interface (eg. RS232 Adapter), if true the node will be ignored on startup</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key,
            Guid needsInterface, Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed,
            bool isWriteable, bool isWriteableFixed, NodeDataType dataType, int maxInstances, bool isAdapterInterface);

        /// <summary>
        /// Creates a new <see cref="NodeTemplate"/>
        /// </summary>
        /// <param name="uid">The unique id</param>
        /// <param name="name">The name used to create the <see cref="NodeInstance.Name"/></param>
        /// <param name="description">The description used to create the <see cref="NodeInstance.Description"/></param>
        /// <param name="key">The key of the <see cref="NodeTemplate"/></param>
        /// <param name="needsInterface">Defines the InterfaceType which is needed to create this <see cref="NodeTemplate"/></param>
        /// <param name="providesInterface">Defines which InterfaceType this <see cref="NodeTemplate"/> provides</param>
        /// <param name="defaultCreated">Defines if the <see cref="NodeTemplate"/> will be created by default when adding the parent node</param>
        /// <param name="isReadable">Defines if the created <see cref="NodeInstance"/> can be read</param>
        /// <param name="isReadableFixed">Defines if the user can change the read flag of the <see cref="NodeInstance"/></param>
        /// <param name="isWriteable">>Defines if the created <see cref="NodeInstance"/> can be written</param>
        /// <param name="isWriteableFixed">Defines if the user can change the write flag of the <see cref="NodeInstance"/></param>
        /// <param name="dataType">Defines the data type which the <see cref="NodeInstance"/> provides. If it is just a container for other nodes set it to <see cref="NodeDataType.NoAttribute"/> </param>
        /// <param name="maxInstances">Defines how many <see cref="NodeInstance"/> can be created from this <see cref="NodeTemplate"/></param>
        /// <param name="isAdapterInterface">defines if the node is just a adapter interface (eg. RS232 Adapter), if true the node will be ignored on startup</param>
        /// <param name="deleteAble">defines if the node can be deleted after creation (should be used for connection state node)</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key,
            Guid needsInterface, Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed,
            bool isWriteable, bool isWriteableFixed, NodeDataType dataType, int maxInstances, bool isAdapterInterface, bool deleteAble);

        /// <summary>
        /// Change <see cref="NodeTemplate"/> meta data
        /// </summary>
        /// <param name="uid">The unique id</param>
        /// <param name="metaName">MetaName to change</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode ChangeNodeTemplateMetaName(Guid uid, string metaName);

        /// <summary>
        /// Change the default visualization template for the <see cref="NodeTemplate"/>
        /// </summary>
        /// <param name="uid">The unique id of the <see cref="NodeTemplate"/></param>
        /// <param name="template"><see cref="VisuMobileObjectTemplateTypes"/> for available templates</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode ChangeDefaultVisuTemplate(Guid uid, VisuMobileObjectTemplateTypes template);

        /// <summary>
        /// Updates a property value in the database
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        CreateTemplateCode SetPropertyValue(Guid uid, object value);
    }
}
