﻿using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.And
{
    public class AndLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("f4a63247-dba5-4659-a3e3-3778c333b684");
        public static readonly Guid RuleInput2 = new Guid("1f3f5d2e-ad2c-4ed4-b12a-71ca62eb4c14");


        public static readonly Guid RuleOutput = new Guid("b90657d9-9c02-4d47-ad8d-b6949e664fa8");

        public override string LogicName => "Logic.And";
        public override Version LogicVersion => new Version(2, 0, 0, 0);

        public override Guid LogicGuid => new Guid("83d04186-4e94-4d57-89a5-22f1181b4ed1");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.AND.NAME", "LOGIC.AND.DESCRIPTION", "logic.and", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "LOGIC.AND.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "LOGIC.AND.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "LOGIC.AND.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AndRule(context);
        }
    }
}
