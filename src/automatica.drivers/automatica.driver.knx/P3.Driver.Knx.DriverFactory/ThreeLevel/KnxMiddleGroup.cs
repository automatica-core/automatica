using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Attributes;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public class KnxMiddleGroup : KnxLevelBase
    {
        public KnxMiddleGroup(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        public sealed override Task WriteValue(IDispatchable source, object value)
        {
            return Task.CompletedTask;
        }

        internal static KnxLevelBase CreateDriverNode(string key, IDriverContext ctx, KnxDriver driver)
        {
            KnxLevelBase attribute = null;
            switch (key)
            {
                case "knx-dpt1":
                    attribute = new KnxDpt1Attribute(ctx, driver);
                    break;
                case "knx-dpt2":
                    attribute = new KnxDpt2Attribute(ctx, driver);
                    break;
                case "knx-dpt3":
                    attribute = new KnxDpt3Attribute(ctx, driver);
                    break;
                case "knx-dpt4":
                    attribute = new KnxDpt4Attribute(ctx, driver);
                    break;
                case "knx-dpt5":
                    attribute = new KnxDpt5Attribute(ctx, driver);
                    break;
                case "knx-dpt6":
                    attribute = new KnxDpt6Attribute(ctx, driver);
                    break;
                case "knx-dpt7":
                    attribute = new KnxDpt7Attribute(ctx, driver);
                    break;
                case "knx-dpt8":
                    attribute = new KnxDpt8Attribute(ctx, driver);
                    break;
                case "knx-dpt9":
                    attribute = new KnxDpt9Attribute(ctx, driver);
                    break;
                case "knx-dpt10":
                    attribute = new KnxDpt10Attribute(ctx, driver);
                    break;
                case "knx-dpt11":
                    attribute = new KnxDpt11Attribute(ctx, driver);
                    break;
                case "knx-dpt13":
                    attribute = new KnxDpt13Attribute(ctx, driver);
                    break;
                case "knx-dpt16":
                    attribute = new KnxDpt16Attribute(ctx, driver);
                    break;
            }

            return attribute;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            KnxLevelBase attribute = CreateDriverNode(ctx.NodeInstance.This2NodeTemplateNavigation.Key, ctx, Driver);
           
            if(attribute == null)
            {
                return null;
            }

            AddChild(attribute);
            return attribute;
        }
    }
}
