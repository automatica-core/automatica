using System;
using System.Collections.Generic;
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
            return _templates.TryGetValue(rorg, out var templates) ? templates : new List<Guid>();
        }

        /// <summary>
        /// The device templates (EEP cases and two-way devices) of a RORG, without the RORG and function containers.
        /// The template guids carry RORG, FUNC and TYPE in bytes 10..12, containers have FUNC or TYPE 0xFF.
        /// </summary>
        public IList<Guid> GetDeviceTemplates(Rorg rorg)
        {
            var result = new List<Guid>();
            foreach (var template in GetTemplates(rorg))
            {
                var bytes = template.ToByteArray();
                if (bytes[11] != 0xFF && bytes[12] != 0xFF)
                {
                    result.Add(template);
                }
            }

            return result;
        }

        /// <returns>the device templates of the profile <paramref name="rorg"/>-<paramref name="func"/>-<paramref name="type"/></returns>
        public IList<Guid> GetDeviceTemplates(Rorg rorg, byte func, byte type)
        {
            var result = new List<Guid>();
            foreach (var template in GetDeviceTemplates(rorg))
            {
                var bytes = template.ToByteArray();
                if (bytes[10] == (byte)rorg && bytes[11] == func && bytes[12] == type)
                {
                    result.Add(template);
                }
            }

            return result;
        }
    }
}
