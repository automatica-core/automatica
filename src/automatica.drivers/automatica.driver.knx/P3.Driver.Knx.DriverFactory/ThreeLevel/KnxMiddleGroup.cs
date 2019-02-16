using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Attributes;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public class KnxMiddleGroup : KnxLevelBase
    {
        public KnxMiddleGroup(IDriverContext driverContext,  KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            KnxLevelBase attribute = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "knx-dpt1":
                    attribute =  new KnxDpt1Attribute(ctx, Driver);
                    break;
                case "knx-dpt2":
                    attribute =  new KnxDpt2Attribute(ctx, Driver);
                    break;
                case "knx-dpt3":
                    attribute =  new KnxDpt3Attribute(ctx, Driver);
                    break;
                case "knx-dpt4":
                    attribute =  new KnxDpt4Attribute(ctx, Driver);
                    break;
                case "knx-dpt5":
                    attribute =  new KnxDpt5Attribute(ctx, Driver);
                    break;
                case "knx-dpt6":
                    attribute =  new KnxDpt6Attribute(ctx, Driver);
                    break;
                case "knx-dpt7":
                    attribute =  new KnxDpt7Attribute(ctx, Driver);
                    break;
                case "knx-dpt8":
                    attribute = new KnxDpt8Attribute(ctx, Driver);
                    break;
                case "knx-dpt9":
                    attribute = new KnxDpt9Attribute(ctx, Driver);
                    break;
                case "knx-dpt10":
                    attribute = new KnxDpt10Attribute(ctx, Driver);
                    break;
                case "knx-dpt11":
                    attribute = new KnxDpt11Attribute(ctx, Driver);
                    break;
                default:
                    return null;
            }


            AddChild(attribute);
            return attribute;
        }
    }
}
