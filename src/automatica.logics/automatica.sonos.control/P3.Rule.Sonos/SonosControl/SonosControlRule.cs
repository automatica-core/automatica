using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Sonos.SonosControl;

public class SonosControlRule : Automatica.Core.Rule.Rule
{
    private readonly IRuleContext _context;

    public SonosControlRule(IRuleContext context) : base(context)
    {
        _context = context;
      
    }

  

    protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
    {
        return new List<IRuleOutputChanged>();
    }
}