using System;

namespace Automatica.Core.WebApi.Tests.Base
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestOrderAttribute : Attribute
    {
        public TestOrderAttribute(int priority)
        {
            Priority = priority;
        }

        public int Priority { get; private set; }
    }
}
