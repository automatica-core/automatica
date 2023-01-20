using Automatica.Core.Driver;
using P3.Driver.MachineFlags.Attributes;

namespace P3.Driver.MachineFlags
{
    public class MachineFlagsDriver : DriverBase
    {
        
        public MachineFlagsDriver(IDriverContext ctx)
            : base(ctx)
        {
        }
        

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;

            switch (key)
            {
                case "machine-flag-binary":
                    return new BinaryFlag(ctx);
                case "machine-flag-number":
                    return new DoubleBinaryFlag(ctx);
                case "machine-flag-string":
                    return new StringBinaryFlag(ctx);
                default:
                    return new Attributes.MachineFlags(ctx);
            }
        }
    }
}
