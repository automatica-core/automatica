using System;
using Automatica.Core.Base.IO;

namespace Automatica.Core.UnitTests.Base.Rules
{
    public class DispatchableMock : IDispatchable
    {
        public DispatchableMock()
        {
            Id = Guid.NewGuid();
            Name = "TestDispatchable";
        }

        public DispatchableMock(string name)
        {

            Id = Guid.NewGuid();
            Name = name;
        }

        public DispatchableSource Source => DispatchableSource.Visualization;

        public DispatchableType Type => DispatchableType.RuleInstance;

        public string Name { get; }

        public Guid Id { get; }
    }
}
