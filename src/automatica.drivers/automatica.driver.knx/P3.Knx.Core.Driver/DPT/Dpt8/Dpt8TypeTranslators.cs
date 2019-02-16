using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace P3.Knx.Core.DPT.Dpt8
{
    internal static class Dpt8TypeTranslators
    {
        internal static readonly IReadOnlyCollection<Type> Types = new ReadOnlyCollection<Type>(new List<Type>
        {
            typeof(Dpt8Translator),
            typeof(Dpt8Type2Translator),
            typeof(Dpt8Type3Translator)
        });
    }
}
