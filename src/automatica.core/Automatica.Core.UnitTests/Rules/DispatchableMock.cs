using Automatica.Core.Base.IO;
using System;

namespace Automatica.Core.UnitTests.Rules
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

        public DispatchableSource Source => DispatchableSource.Visualisation;

        public DispatchableType Type => DispatchableType.RuleInstance;

        public string Name { get; }

        public Guid Id { get; }
    }
}
