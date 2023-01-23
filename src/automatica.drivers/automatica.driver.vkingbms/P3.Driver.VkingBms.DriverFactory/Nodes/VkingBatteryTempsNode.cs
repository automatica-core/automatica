using Automatica.Core.Driver;

namespace P3.Driver.VkingBms.DriverFactory.Nodes
{
    internal class VkingBatteryTempsNode : DriverBase
    {
        internal VkingBatteryValueNode Environment { get; private set; }
        internal VkingBatteryValueNode Mos { get; private set; }
        internal VkingBatteryValueNode CellT1 { get; private set; }
        internal VkingBatteryValueNode CellT2 { get; private set; }
        internal VkingBatteryValueNode CellT3 { get; private set; }
        internal VkingBatteryValueNode CellT4 { get; private set; }
        public VkingBatteryTempsNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key.Replace("vking-bms-pack-cells-", "");

            IDriverNode? ret = null;
            switch (key)
            {
                case "temp_env":
                    ret = Environment = new VkingBatteryValueNode(ctx);
                    break;
                case "temp_mos":
                    ret = Mos = new VkingBatteryValueNode(ctx);
                    break;
                case "temp_cell_t1":
                    ret = CellT1 = new VkingBatteryValueNode(ctx);
                    break;
                case "temp_cell_t2":
                    ret = CellT2 = new VkingBatteryValueNode(ctx);
                    break;
                case "temp_cell_t3":
                    ret = CellT3 = new VkingBatteryValueNode(ctx);
                    break;
                case "temp_cell_t4":
                    ret = CellT4 = new VkingBatteryValueNode(ctx);
                    break;
            }

            return ret;
        }
    }
}
