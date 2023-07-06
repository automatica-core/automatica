using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.StringOperations.Concat
{
    public class StringConcatFactory : LogicFactory
    {
        public override string LogicName => "StringOperations";

        public override Guid LogicGuid => new Guid("c7a58432-159f-449b-88fc-8542d9c5dde9");

        public static readonly Guid RuleInput1 = new Guid("fb8d85eb-85d0-47a0-b47e-8c7612975084");
        public static readonly Guid RuleInput2 = new Guid("325a3d6b-28fc-4b77-804b-c4aba85bf60b");
        public static readonly Guid RuleInput3 = new Guid("5242b41f-5a62-4e61-b998-03afe5b78dcc");
        public static readonly Guid RuleInput4 = new Guid("b451ed6a-0bb8-49c1-ac8a-2bd887abfdb3");

        public static readonly Guid RuleOutput = new Guid("2b39db96-d482-4143-b02e-ced03bb90896");

        public override Version LogicVersion => new Version(0, 0, 0, 2);

        public override bool InDevelopmentMode => true;

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new StringConcat(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "STRING_OPERATIONS.CONCAT.NAME", "STRING_OPERATIONS.CONCAT.DESCRIPTION",
               "string_operations_concat", "STRING_OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "STRING_OPERATIONS.CONCAT.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "STRING_OPERATIONS.CONCAT.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleInput3, "I3", "STRING_OPERATIONS.CONCAT.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);
            factory.CreateLogicInterfaceTemplate(RuleInput4, "I4", "STRING_OPERATIONS.CONCAT.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 4);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "STRING_OPERATIONS.CONCAT.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

        }
    }
}
