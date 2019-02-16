using System;
using System.ComponentModel;
using Automatica.Core.Base.IO;

namespace Automatica.Core.UnitTests.Common
{
    public class DispatchableMock : IDispatchable
    {
        public DispatchableType Type => DispatchableType.NodeInstance;
        public string Name => "Test";
        public Guid Id => new Guid("29fc45f1-f598-4aa1-b3ef-de49f2eb409a");

        public static DispatchableMock Instance { get; } = new DispatchableMock();

        public DispatchableSource Source => DispatchableSource.NodeInstance;
    }
}
