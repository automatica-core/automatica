using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Templates
{
    public class VisuMobileTemplateFactory : PropertyTemplateFactory, IVisuTemplateFactory
    {
        public VisuMobileTemplateFactory(AutomaticaContext database, IConfiguration config) : base(database, config,
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
