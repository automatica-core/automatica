using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.DriverFactory.Templates
{
    public class EnOceanTemplateFactory
    {
        private readonly Dictionary<Rorg, List<Guid>> _templates;
        public EnOceanTemplateFactory()
        {
            _templates = new Dictionary<Rorg, List<Guid>>();
            _templates.Add(Rorg.Rps, new List<Guid>());
            _templates.Add(Rorg.OneBs, new List<Guid>());
            _templates.Add(Rorg.FourBs, new List<Guid>());
            _templates.Add(Rorg.Vld, new List<Guid>());
        }

        public void AddTemplate(Rorg rorg, Guid template)
        {
            _templates[rorg].Add(template);
        }

        public void AddTemplate(byte rorg, Guid template)
        {
            switch (rorg)
            {
                case 0xF6:
                    AddTemplate(Rorg.Rps, template);
                    break;
                case 0xD5:
                    AddTemplate(Rorg.OneBs, template);
                    break;
                case 0xA5:
                    AddTemplate(Rorg.FourBs, template);
                    break;
                case 0xD2:
                    AddTemplate(Rorg.Vld, template);
                    break;
            }
        }

        public IList<Guid> GetTemplates(Rorg rorg)
        {
            return _templates[rorg];
        }
    }
}
