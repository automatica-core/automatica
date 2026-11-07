using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using Xunit;

namespace P3.Logic.Operations.Tests
{
    /// <summary>
    /// An <see cref="IDispatchable"/> that does not come from the visualization (e.g. a linked node or another logic)
    /// </summary>
    internal sealed class NonVisuDispatchable : IDispatchable
    {
        public DispatchableSource Source => DispatchableSource.RuleInstance;
        public DispatchableType Type => DispatchableType.RuleInstance;
        public string Name => "NonVisu";
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsRemanent => false;
    }

    internal static class OperationsTestHelper
    {
        /// <summary>
        /// Returns the single output value emitted for the given output template
        /// </summary>
        public static object Output(IList<ILogicOutputChanged> outputs, Guid outputTemplate)
        {
            var matches = outputs.Where(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == outputTemplate).ToList();
            Assert.Single(matches);
            return matches[0].Value;
        }

        public static bool HasOutput(IList<ILogicOutputChanged> outputs, Guid outputTemplate)
        {
            return outputs.Any(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == outputTemplate);
        }

        public static object DispatchedValue(IDispatcher dispatcher, Guid outputTemplate)
        {
            var values = dispatcher.GetValues(DispatchableType.RuleInstance);
            return values.TryGetValue(outputTemplate, out var value) ? value.Value : null;
        }

        /// <summary>
        /// Polls the dispatcher until the output has the expected value or the deadline is reached
        /// </summary>
        public static async Task<bool> WaitForDispatchedValue(IDispatcher dispatcher, Guid outputTemplate, object expected, int timeoutMs = 3000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (Equals(DispatchedValue(dispatcher, outputTemplate), expected))
                {
                    return true;
                }
                await Task.Delay(5);
            }

            return Equals(DispatchedValue(dispatcher, outputTemplate), expected);
        }
    }
}
