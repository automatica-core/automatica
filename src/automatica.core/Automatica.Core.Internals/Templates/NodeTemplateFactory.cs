using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Templates
{
    public class NodeTemplateFactory : PropertyTemplateFactory, INodeTemplateFactory
    {
        public NodeTemplateFactory(AutomaticaContext database, IConfiguration config) : base (database, config, (template, guid) => template.This2NodeTemplate = guid)
        {
        }
        
        public NodeTemplate GetNodeTemplateById(Guid id)
        {
            var db = Db;
            var x = db.NodeTemplates
                .Include(a => a.This2NodeDataTypeNavigation)
                .Include(a => a.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Single(a => a.ObjId == id);
            return x;
        }

        public NodeTemplate GetNodeTemplateByKey(string key)
        {
            var db = Db;
            var x = db.NodeTemplates
                .Include(a => a.This2NodeDataTypeNavigation)
                .Include(a => a.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Single(a => a.Key == key);
            return x;
        }

        public ICollection<NodeTemplate> GetNodeTemplates(params Guid[] key)
        {
            var templates = Db.NodeTemplates
                .Include(a => a.This2NodeDataTypeNavigation)
                .Include(a => a.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Where(a => key.Contains(a.ObjId)).ToList();

            return templates;
        }

        private ICollection<NodeTemplate> GetDefaultChildNodeTemplates(NodeTemplate nodeTemplate)
        {
            var templates = Db.NodeTemplates
                .Include(a => a.This2NodeDataTypeNavigation)
                .Include(a => a.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Where(a => a.DefaultCreated & a.NeedsInterface2InterfacesType == nodeTemplate.ProvidesInterface2InterfaceType).ToList();

            return templates;
        }

        public NodeInstance CreateNodeInstanceByKey(string key)
        {
            return CreateNodeInstance(GetNodeTemplateByKey(key));
        }

        public NodeInstance CreateNodeInstance(NodeTemplate template)
        {
            var node = NodeInstanceFactory.CreateNodeInstanceFromTemplate(template);

            var childrens = GetDefaultChildNodeTemplates(template);

            foreach (var child in childrens)
            {
                node.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(child));
            }

            return node;
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
           
            interfaceType.Name = name;
            interfaceType.Description = description;
            interfaceType.MaxChilds = maxChilds;
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
                SystemLogger.Instance.LogError($"Could not create node template {uid}-{name}.{key} {e}");

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
