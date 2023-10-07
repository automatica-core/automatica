using System;
using Automatica.Core.Driver;

namespace P3.Driver.VkingBms.DriverFactory.Nodes
{
    internal class VkingBatteryCellsNode : DriverNoneAttributeBase
    {
        internal VkingBatteryValueNode Cell1 { get; private set; }
        internal VkingBatteryValueNode Cell2 { get; private set; }
        internal VkingBatteryValueNode Cell3 { get; private set; }
        internal VkingBatteryValueNode Cell4 { get; private set; }
        internal VkingBatteryValueNode Cell5 { get; private set; }
        internal VkingBatteryValueNode Cell6 { get; private set; }
        internal VkingBatteryValueNode Cell7 { get; private set; }
        internal VkingBatteryValueNode Cell8 { get; private set; }
        internal VkingBatteryValueNode Cell9 { get; private set; }
        internal VkingBatteryValueNode Cell10 { get; private set; }
        internal VkingBatteryValueNode Cell11 { get; private set; }
        internal VkingBatteryValueNode Cell12 { get; private set; }
        internal VkingBatteryValueNode Cell13 { get; private set; }
        internal VkingBatteryValueNode Cell14 { get; private set; }
        internal VkingBatteryValueNode Cell15 { get; private set; }
        internal VkingBatteryValueNode Cell16 { get; private set; }
        public VkingBatteryCellsNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key.Replace("vking-bms-pack-cells-", "");

            IDriverNode? ret = null;
            switch (key)
            {
                case "cell_1":
                    ret = Cell1 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_2":
                    ret = Cell2 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_3":
                    ret = Cell3 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_4":
                    ret = Cell4 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_5":
                    ret = Cell5 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_6":
                    ret = Cell6 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_7":
                    ret = Cell7 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_8":
                    ret = Cell8 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_9":
                    ret = Cell9 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_10":
                    ret = Cell10 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_11":
                    ret = Cell11 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_12":
                    ret = Cell12 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_13":
                    ret = Cell13 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_14":
                    ret = Cell14 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_15":
                    ret = Cell15 = new VkingBatteryValueNode(ctx);
                    break;
                case "cell_16":
                    ret = Cell16 = new VkingBatteryValueNode(ctx);
                    break;

            }

            return ret;
        }
    }
}
