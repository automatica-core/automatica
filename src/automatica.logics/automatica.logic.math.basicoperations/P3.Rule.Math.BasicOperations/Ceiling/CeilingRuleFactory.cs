using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Ceiling
{
    public class CeilingLogicFactory : LogicFactory
    {
        /// <summary>
        /// unique guid for the input value
        /// </summary>
        public static readonly Guid RuleInput1 = new Guid("38b8e905-ece3-4878-96f3-22466e779099");

        /// <summary>
        /// unique id for the output value
        /// </summary>
        public static readonly Guid RuleOutput = new Guid("eeac47eb-ad93-432c-8317-c9ee7e322d22");

        /// <summary>
        /// Rule name, used for logging
        /// </summary>
        public override string LogicName => "Math.Ceiling";

        /// <summary>
        /// Version
        /// </summary>
        public override Version LogicVersion => new Version(2, 0, 0, 0);

        /// <summary>
        /// Unique id for the rule block
        /// </summary>
        public override Guid LogicGuid => new Guid("0cdecb57-326a-4f8d-9474-e31cb515964c");


        /// <summary>
        /// Init method for your rule design
        /// </summary>
        /// <param name="factory"></param>
        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.CEILING.NAME", "MATH.CEILING.DESCRIPTION",
                "math.ceiling", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.CEILING.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.CEILING.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        /// <summary>
        /// Init method to create a instance of your rule
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new CeilingRule(context);
        }
    }
}
