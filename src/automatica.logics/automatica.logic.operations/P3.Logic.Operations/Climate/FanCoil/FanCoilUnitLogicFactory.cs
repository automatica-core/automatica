using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Climate.FanCoil
{
    public class FanCoilUnitLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("f5ffcf8f-3c97-4d44-a595-5ceda34fe308");
        public static readonly FanCoilIds Ids = new(Id);

        public override string LogicName => "Climate.FanCoilUnit";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            FanCoilIds.CreateTemplates(factory, Id, "FAN_COIL_UNIT", "climate.fan-coil-unit", false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new FanCoilLogic(context, Ids, false);
    }

    public class FanCoilFreshAirLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("bdd2a211-4650-43ce-b9b8-9cd6e00d9cb8");
        public static readonly FanCoilIds Ids = new(Id);

        public override string LogicName => "Climate.FanCoilFreshAir";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            FanCoilIds.CreateTemplates(factory, Id, "FAN_COIL_FRESH_AIR", "climate.fan-coil-fresh-air", true);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new FanCoilLogic(context, Ids, true);
    }
}
