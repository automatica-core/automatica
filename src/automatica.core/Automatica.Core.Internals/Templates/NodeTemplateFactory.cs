﻿using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Templates
{
    public class NodeTemplateFactoryProvider(IServiceProvider serviceProvider)
        : TemplateFactoryProvider<NodeTemplateFactory>(serviceProvider)
    {
        protected override NodeTemplateFactory CreateFactory(Guid owner, IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            //create a new instance in purpose - we don't want to share the same instance between different owners
            var databaseContext = new AutomaticaContext(config);
            return new NodeTemplateFactory(
                serviceProvider.GetRequiredService<ILogger<NodeTemplateFactory>>(),
                databaseContext, config, serviceProvider.GetRequiredService<INodeInstanceService>());
        }
    }
    
    public class NodeTemplateFactory : PropertyTemplateFactory, INodeTemplateFactory
    {
        private readonly INodeInstanceService _nodeInstanceService;

        public NodeTemplateFactory(ILogger<NodeTemplateFactory> logger, AutomaticaContext database, IConfiguration config, INodeInstanceService nodeInstanceService) : base (logger, database, config, (template, guid) => template.This2NodeTemplate = guid)
        {
            _nodeInstanceService = nodeInstanceService;
        }
        
        public NodeTemplate GetNodeTemplateById(Guid id)
        {
            return _nodeInstanceService.GetTemplateById(id);
        }

        public NodeTemplate GetNodeTemplateByKey(string key)
        {
            return _nodeInstanceService.GetTemplateByKey(key);
        }

        public ICollection<NodeTemplate> GetNodeTemplates(params Guid[] key)
        {
            return _nodeInstanceService.GetTemplatesById(key);
        }

        public NodeInstance CreateNodeInstance(string locale, Guid template)
        {
            return CreateNodeInstance(locale, GetNodeTemplateById(template));
        }

        public NodeInstance CreateNodeInstance(string locale, NodeTemplate template)
        {
            return _nodeInstanceService.CreateNodeInstance(locale, template);
        }

        public NodeInstance CreateNodeInstanceByKey(string key)
        {
            return CreateNodeInstance(GetNodeTemplateByKey(key));
        }

        public NodeInstance CreateNodeInstance(NodeTemplate template)
        {
            return CreateNodeInstance(null, template);
        }

        public NodeInstance CreateNodeInstance(Guid template)
        {
            return CreateNodeInstance(GetNodeTemplateById(template));
        }

        public CreateTemplateCode CreateBoard(Guid uid, string name, string description)
        {
            throw new NotImplementedException();
        }

        public CreateTemplateCode CreateBoardInterface(Guid uid, string name, string description, Guid boardGuid, string meta, Guid intefaceType)
        {
            var boardInterface = Db.BoardInterfaces.SingleOrDefault(a => a.ObjId == uid);
            var retValue = CreateTemplateCode.Updated;
            if (boardInterface == null)
            {
                boardInterface = new BoardInterface();
                boardInterface.ObjId = uid;
                boardInterface.This2BoardType = boardGuid;
                retValue = CreateTemplateCode.Created;
            }
            boardInterface.Name = name;
            boardInterface.Description = description;
            boardInterface.This2InterfaceType = intefaceType;
            boardInterface.Meta = meta;

            if (retValue == CreateTemplateCode.Created)
            {
                Db.BoardInterfaces.Add(boardInterface);
            }
            else
            {
                Db.BoardInterfaces.Update(boardInterface);
            }

            return retValue;
        }

        public CreateTemplateCode CreateBoardInterfaceForAllBoards(string name, string description, string meta, Guid intefaceType)
        {
            var retValue = CreateTemplateCode.Updated;

            foreach (var board in Db.BoardTypes)
            {
                var boardInterface = Db.BoardInterfaces.SingleOrDefault(a => a.This2InterfaceType == intefaceType);
              
                if (boardInterface == null)
                {
                    boardInterface = new BoardInterface();
                    boardInterface.ObjId = Guid.NewGuid();
                    boardInterface.This2BoardType = board.Type;
                    retValue = CreateTemplateCode.Created;
                }
                boardInterface.Name = name;
                boardInterface.Description = description;
                boardInterface.This2InterfaceType = intefaceType;
                boardInterface.Meta = meta;

                if (retValue == CreateTemplateCode.Created)
                {
                    Db.BoardInterfaces.Add(boardInterface);
                }
                else
                {
                    Db.BoardInterfaces.Update(boardInterface);
                }
                
            }
            return retValue;
        }

        public CreateTemplateCode CreateInterfaceType(Guid uid, string name, string description, int maxChilds, int maxInstances,
            bool isDriverInterface)
        {
            var interfaceType = Db.InterfaceTypes.SingleOrDefault(a => a.Type == uid);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (interfaceType == null)
            {
                interfaceType = new InterfaceType();
                interfaceType.Type = uid;
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }
            if (interfaceType.Owner.HasValue && interfaceType.Owner != Owner && !AllowOwnerOverride)
            {
                throw new ArgumentException("You are not allowed to modify this template...");
            }

            interfaceType.Owner = Owner;

            interfaceType.Name = name;
            interfaceType.Description = description;
            interfaceType.MaxChilds = maxChilds;
            interfaceType.FactoryReference = Factory.FactoryGuid;
            interfaceType.MaxInstances = maxInstances;
            interfaceType.IsDriverInterface = isDriverInterface;

            if (isNewObject)
            {
                Db.InterfaceTypes.Add(interfaceType);
            }
            else
            {
                Db.InterfaceTypes.Update(interfaceType);
            }

            return retValue;
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key,
            Guid needsInterface,
            Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable,
            bool isWriteableFixed, Base.Templates.NodeDataType dataType, int maxInstances, bool isAdapterInterface)
        {
            return CreateNodeTemplate(uid, name, description, key, needsInterface, providesInterface, defaultCreated,
                isReadable, isReadableFixed, isWriteable, isWriteableFixed, dataType, maxInstances, isAdapterInterface,
                true);
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key,
           Guid needsInterface,
           Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable,
           bool isWriteableFixed, Base.Templates.NodeDataType dataType, int maxInstances, bool isAdapterInterface, bool deleteAble)
        {
            try
            {
                var nodeTemplate = Db.NodeTemplates.SingleOrDefault(p => p.ObjId == uid);
                var retValue = CreateTemplateCode.Updated;

                bool isNewObject = false;
                if (nodeTemplate == null)
                {
                    isNewObject = true;
                    nodeTemplate = new NodeTemplate();
                    nodeTemplate.ObjId = uid;
                    retValue = CreateTemplateCode.Created;
                }
                if (nodeTemplate.Owner.HasValue && nodeTemplate.Owner != Owner && !AllowOwnerOverride)
                {
                    throw new ArgumentException("You are not allowed to modify this template...");
                }

                nodeTemplate.Owner = Owner;


                nodeTemplate.FactoryReference = Factory.FactoryGuid;
                nodeTemplate.Name = name;
                nodeTemplate.Description = description;
                nodeTemplate.Key = key;
                nodeTemplate.This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label);
                nodeTemplate.NeedsInterface2InterfacesType = needsInterface;
                nodeTemplate.ProvidesInterface2InterfaceType = providesInterface;
                nodeTemplate.DefaultCreated = defaultCreated;
                nodeTemplate.IsReadable = isReadable;
                nodeTemplate.IsReadableFixed = isReadableFixed;
                nodeTemplate.IsWriteable = isWriteable;
                nodeTemplate.IsWriteableFixed = isWriteableFixed;
                nodeTemplate.This2NodeDataType = (long)dataType;
                nodeTemplate.MaxInstances = maxInstances;
                nodeTemplate.IsAdapterInterface = isAdapterInterface;
                nodeTemplate.IsDeleteable = deleteAble;

                if (isNewObject)
                {
                    Db.NodeTemplates.Add(nodeTemplate);
                }
                else
                {
                    Db.NodeTemplates.Update(nodeTemplate);
                }
                
                return retValue;
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not create node template {uid}-{name}.{key} {e}");

                throw;
            }
        }

        public CreateTemplateCode ChangeNodeTemplateMetaName(Guid uid, string metaName)
        {
            var nodeTemplate = Db.NodeTemplates.SingleOrDefault(p => p.ObjId == uid);
            if (nodeTemplate == null)
            {
                Db.SaveChanges();
                nodeTemplate = Db.NodeTemplates.SingleOrDefault(p => p.ObjId == uid);

                if (nodeTemplate == null)
                {
                    return CreateTemplateCode.Error;
                }
            }

            nodeTemplate.NameMeta = metaName;


            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode ChangeDefaultVisuTemplate(Guid uid, VisuMobileObjectTemplateTypes template)
        {
            var nodeTemplate = Db.NodeTemplates.SingleOrDefault(p => p.ObjId == uid);
            if (nodeTemplate == null)
            {
                Db.SaveChanges();
                nodeTemplate = Db.NodeTemplates.SingleOrDefault(p => p.ObjId == uid);

                if (nodeTemplate == null)
                {
                    return CreateTemplateCode.Error;
                }
            }

            nodeTemplate.This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(template);

            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode SetPropertyValue(Guid uid, object value)
        {
            var property = Db.PropertyInstances.Where(a => a.ObjId == uid).Include(a => a.This2PropertyTemplateNavigation).Include(a => a.This2PropertyTemplateNavigation.This2PropertyTypeNavigation).FirstOrDefault();

            if (property == null)
            {
                return CreateTemplateCode.Error;
            }


            property.Value = value;
            Db.SaveChanges();
            return CreateTemplateCode.Updated;
        }


 
    }
}
