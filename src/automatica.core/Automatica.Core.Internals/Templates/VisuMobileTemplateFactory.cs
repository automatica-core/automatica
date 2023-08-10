﻿using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Templates
{
    public class VisuMobileFactory : IFactory
    {
        public Guid FactoryGuid => new Guid("58df0a39-e110-42cc-a540-88d7e1dfa0a0");
    }

    public class VisuMobileTemplateFactory : PropertyTemplateFactory, IVisuTemplateFactory
    {
        public VisuMobileTemplateFactory(ILogger logger, AutomaticaContext database, IConfiguration config) : base(logger, database, config,
            (template, guid) => template.This2VisuObjectTemplate = guid)
        {

        }

        public CreateTemplateCode CreateVisuMobileTemplate(Guid uid, string name, string description, string key,
            string group, int height, int width, bool isVisibleForUser)
        {
            var visuObjectTemplate = Db.VisuObjectTemplates.SingleOrDefault(a => a.ObjId == uid);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (visuObjectTemplate == null)
            {
                visuObjectTemplate = new VisuObjectTemplate();
                visuObjectTemplate.ObjId = uid;
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }

            visuObjectTemplate.Name = name;
            visuObjectTemplate.Description = description;
            visuObjectTemplate.Key = key;
            visuObjectTemplate.Group = group;
            visuObjectTemplate.Height = height;
            visuObjectTemplate.Width = width;
            visuObjectTemplate.IsVisibleForUser = isVisibleForUser;

            visuObjectTemplate.This2VisuPageType = (int) VisuPageTypeEnumeration.Mobile;

            if (isNewObject)
            {
                Db.VisuObjectTemplates.Add(visuObjectTemplate);
            }
            else
            {
                Db.VisuObjectTemplates.Update(visuObjectTemplate);
            }

            Db.SaveChanges(true);
            return retValue;
        }

        public CreateTemplateCode UpdateMaxMinValues(Guid uid, float? maxHeight, float? maxWidth, float? minHeight, float? minWidth)
        {
            var visuObjectTemplate = Db.VisuObjectTemplates.SingleOrDefault(a => a.ObjId == uid);

            if (visuObjectTemplate == null)
            {
                return CreateTemplateCode.Error;
            }

            visuObjectTemplate.MaxHeight = maxHeight;
            visuObjectTemplate.MaxWidth = maxWidth;
            visuObjectTemplate.MinWidth = minWidth;
            visuObjectTemplate.MinHeight = minHeight;

            Db.VisuObjectTemplates.Update(visuObjectTemplate);
            Db.SaveChanges(true);

            return CreateTemplateCode.Updated;
        }
    }
}
