using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace P3.Knx.Core.DPT.Dpt7
{
    internal static class Dpt7TypeTranslators
    {
        internal static readonly IReadOnlyCollection<Type> Types = new ReadOnlyCollection<Type>(new List<Type>
        {
            typeof(Dpt7001TypeTranslator),
            typeof(Dpt7003TypeTranslator),
            typeof(Dpt7004TypeTranslator)
        });
    }  
}
