# Automatica.Core.Rule
## How to
The RuleFactory is your entry point to Automtaica.Core. See the [API](/api/Automatica.Core.Rule.RuleFactory.html).

Here is a simple example on how to create a rule block using a ceileing function.

### RuleFactory
In the RuleFactory you design how the rule block looks. How many & which inputs, outputs and parameters.

```C#
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
    /// Init method to provide localization data
    /// </summary>
    /// <param name="provider"></param>
    public override void InitLocalization(ILocalizationProvider provider)
    {
        string directory = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
        provider.AddLocalization(Path.Combine(directory, "Math-de.json"), CultureInfo.GetCultureInfo("de-DE"));
        provider.AddLocalization(Path.Combine(directory, "Math-en.json"), CultureInfo.GetCultureInfo("en-US"));
    }

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
```

### Rule implementation
```C#
public class CeilingRule : Automatica.Core.Rule.Rule
{
    private double _i1 = 0.0;

    private readonly RuleInterfaceInstance _output;

    public CeilingRule(IRuleContext context) : base(context)
    {
        _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
            a.This2RuleInterfaceTemplate == CeilingRuleFactory.RuleOutput);
    }

    /// <summary>
    /// Callback when a input value has changed, you can calculate multiple output values at once, and return a list of changed output values
    /// </summary>
    /// <param name="instance">The input value which has changed</param>
    /// <param name="source">The source from where the value is dispatched</param>
    /// <param name="value">The value itself</param>
    /// <returns></returns>
    protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
    {
        if (instance.This2RuleInterfaceTemplate == CeilingRuleFactory.RuleInput1 && value != null)
        {
            _i1 = Convert.ToDouble(value);
        }

        return SingleOutputChanged(new RuleOutputChanged(_output,  System.Math.Ceiling(_i1)));
    }
}
```
