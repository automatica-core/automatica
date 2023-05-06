using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace P3.Knx.Core.Driver.DPT.Dpt9
{
    internal static class Dpt9TypeTranslators
    {
        internal static readonly IReadOnlyCollection<Type> Types = new ReadOnlyCollection<Type>(new List<Type>
        {
            typeof(Dpt9001TypeTranslator),
            typeof(Dpt9Type2),
            typeof(Dpt9004TypeTranslator),
            typeof(Dpt9026TypeTranslator),
            typeof(Dpt9027TypeTranslator),
            typeof(Dpt9028TypeTranslator)
        });
    } 
}
