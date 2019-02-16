﻿using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Ceiling
{
    public class CeilingRuleFactory : RuleFactory
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
        public override string RuleName => "Math.Ceiling";

        /// <summary>
        /// Version
        /// </summary>
        public override Version RuleVersion => new Version(1, 0, 0, 0);

        /// <summary>
        /// Unique id for the rule block
        /// </summary>
        public override Guid RuleGuid => new Guid("0cdecb57-326a-4f8d-9474-e31cb515964c");


        /// <summary>
        /// Init method for your rule design
        /// </summary>
        /// <param name="factory"></param>
        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MATH.CEILING.NAME", "MATH.CEILING.DESCRIPTION",
                "math.ceiling", "MATH.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "MATH.CEILING.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "MATH.CEILING.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        /// <summary>
        /// Init method to create a instance of your rule
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new CeilingRule(context);
        }
    }
}
