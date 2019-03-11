using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Attributes;
using P3.Knx.Core.Abstractions;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public class KnxMiddleGroup : KnxLevelBase
    {
        public KnxMiddleGroup(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        internal static KnxLevelBase CreateDriverNode(string key, IDriverContext ctx, IKnxDriver driver)
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
                default:
                    return null;
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
