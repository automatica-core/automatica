using System;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.Templates
{
    /// <summary>
    /// LogicInterfaceDirection - either input,output or parameters
    /// </summary>
    public enum LogicInterfaceDirection
    {
        Input  = 1,
        Output,
        Param
    }

    /// <summary>
    /// Interface for creating <see cref="RuleTemplate"/>
    /// </summary>
    public interface ILogicTemplateFactory : IPropertyTemplateFactory
    {
        /// <summary>
        /// Creates a <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="name">The name of the <see cref="RuleTemplate"/></param>
        /// <param name="description">The description of the <see cref="RuleTemplate"/></param>
        /// <param name="key">A unique key for the <see cref="RuleTemplate"/></param>
        /// <param name="group">Group key for the <see cref="RuleTemplate"/></param>
        /// <param name="height">Height in rule editor</param>
        /// <param name="width">Width in rule editor</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateLogicTemplate(Guid id, string name, string description, string key, string group,
            double height, double width);

        /// <summary>
        /// Creates a <see cref="RuleInterfaceTemplate"/> for the <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="name">Name of the <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="description">Description of the  <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="direction">The direction, <see cref="LogicInterfaceDirection"/></param>
        /// <param name="maxLinks">Defines how many links can be made</param>
        /// <param name="sortOrder">Sort order of all  <see cref="RuleInterfaceTemplate"/></param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder);

        /// <summary>
        /// Creates a <see cref="RuleInterfaceTemplate"/> for the <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="name">Name of the <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="description">Description of the  <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="key">Defines a unique key for the interface template<see cref="RuleInterfaceTemplate"/></param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="direction">The direction, <see cref="LogicInterfaceDirection"/></param>
        /// <param name="maxLinks">Defines how many links can be made</param>
        /// <param name="sortOrder">Sort order of all  <see cref="RuleInterfaceTemplate"/></param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, string key, Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder);

        /// <summary>
        /// Creates a <see cref="RuleInterfaceTemplate"/> for the <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="name">Name of the <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="description">Description of the  <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="direction">The direction, <see cref="LogicInterfaceDirection"/></param>
        /// <param name="maxLinks">Defines how many links can be made</param>
        /// <param name="sortOrder">Sort order of all  <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="type">Type type of the interface <see cref="RuleInterfaceType"/></param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder, RuleInterfaceType type);

        /// <summary>
        /// Creates a <see cref="RuleInterfaceTemplate"/> for the <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="name">Name of the <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="description">Description of the  <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="key">Defines a unique key for the interface template<see cref="RuleInterfaceTemplate"/></param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="direction">The direction, <see cref="LogicInterfaceDirection"/></param>
        /// <param name="maxLinks">Defines how many links can be made</param>
        /// <param name="sortOrder">Sort order of all  <see cref="RuleInterfaceTemplate"/></param>
        /// <param name="type">Type type of the interface <see cref="RuleInterfaceType"/></param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, string key,
            Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder, RuleInterfaceType type);

        /// <summary>
        /// Creates a parameter for the  <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">Unique id for the parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="sortOrder">Sort order of all parameters</param>
        /// <param name="dataType">Data type of the parameter, <see cref="RuleInterfaceParameterDataType"/></param>
        /// <param name="defaultValue">Default value of the parameter</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue);

        /// <summary>
        /// Creates a parameter for the  <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">Unique id for the parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="key">Key of the parameter</param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="sortOrder">Sort order of all parameters</param>
        /// <param name="dataType">Data type of the parameter, <see cref="RuleInterfaceParameterDataType"/></param>
        /// <param name="defaultValue">Default value of the parameter</param>
        /// <parma name="linkable">Defines if the parameter is linkable in the editor</parma>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, string key, Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue, bool linkable);

        /// <summary>
        /// Creates a parameter for the  <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">Unique id for the parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="key">Key of the parameter</param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="sortOrder">Sort order of all parameters</param>
        /// <param name="dataType">Data type of the parameter, <see cref="RuleInterfaceParameterDataType"/></param>
        /// <param name="defaultValue">Default value of the parameter</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, string key, Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue);

        /// <summary>
        /// Creates a parameter for the  <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">Unique id for the parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="sortOrder">Sort order of all parameters</param>
        /// <param name="dataType">Data type of the parameter, <see cref="RuleInterfaceParameterDataType"/></param>
        /// <param name="defaultValue">Default value of the parameter</param>
        /// <parma name="linkable">Defines if the parameter is linkable in the editor</parma>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue, bool linkable);


        /// <summary>
        /// Creates a parameter for the  <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">Unique id for the parameter</param>
        /// <param name="name">Name of the parameter</param>
        /// <param name="description">Description of the parameter</param>
        /// <param name="key">Key of the parameter</param>
        /// <param name="ruleTemplate">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="sortOrder">Sort order of all parameters</param>
        /// <param name="dataType">Data type of the parameter, <see cref="RuleInterfaceParameterDataType"/></param>
        /// <param name="defaultValue">Default value of the parameter</param>
        /// <parma name="linkable">Defines if the parameter is linkable in the editor</parma>
        /// <parma name="meta">Defines the meta information for the property grid</parma>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, string key, Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue, bool linkable, string meta);



        /// <summary>
        /// Change the default visualization template for a <see cref="RuleTemplate"/>
        /// </summary>
        /// <param name="id">Unique id of the <see cref="RuleTemplate"/></param>
        /// <param name="template"><see cref="VisuMobileObjectTemplateTypes"/> for available templates</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode ChangeDefaultVisuTemplate(Guid id, VisuMobileObjectTemplateTypes template);

        /// <summary>
        /// Returns the <see cref="RuleTemplate"/> from the given id. Returns null if not found
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        RuleTemplate GetById(Guid id);

        /// <summary>
        /// Creates a new <see cref="RuleInstance"/> from the given template id
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns></returns>
        RuleInstance CreateLogicInstance(Guid templateId);

        /// <summary>
        /// Creates a new <see cref="RuleInstance"/> from the given template.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        RuleInstance CreateLogicInstance(RuleTemplate template);
    }
}
