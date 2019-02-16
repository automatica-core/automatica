using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace P3.Driver.Times.DriverFactory
{
    public abstract class BaseTimesDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public override string DriverName => "Times";

        public static double Latitude { get; private set; }
        public static double Longitude { get; private set; }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.AddSettingsEntry("Longitude", null, "Geo", PropertyTemplateType.Numeric, true);
            factory.AddSettingsEntry("Latitude", null, "Geo", PropertyTemplateType.Numeric, true);


            var valueDouble = factory.GetSetting("Latitude").ValueDouble;
            if (valueDouble != null)
            {
                Latitude = valueDouble.Value;
                var d = factory.GetSetting("Longitude").ValueDouble;
                if (d != null)
                    Longitude = d.Value;
            }
        }
    }

}
