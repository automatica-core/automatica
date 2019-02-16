using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace P3.Knx.Core.DPT.Dpt5
{
    internal static class Dpt5TypeTranslators
    {
        internal static readonly IReadOnlyCollection<Type> Types = new ReadOnlyCollection<Type>(new List<Type>
        {
            typeof(Dpt5001TypeTranslator),
            typeof(Dpt5003TypeTranslator),
            typeof(Dpt5006TypeTranslator)
        });
    }

    
  
}
