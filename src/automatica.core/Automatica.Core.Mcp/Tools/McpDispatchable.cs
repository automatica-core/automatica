using Automatica.Core.Base.IO;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// Sender of a value written through MCP, dispatched like a write from the visualization.
    /// </summary>
    internal sealed class McpDispatchable : IDispatchable
    {
        public McpDispatchable(string name, Guid id, bool isRemanent)
        {
            Name = name;
            Id = id;
            IsRemanent = isRemanent;
        }

        public DispatchableSource Source => DispatchableSource.Visualization;
        public DispatchableType Type => DispatchableType.Visualization;
        public string Name { get; }
        public Guid Id { get; }
        public bool IsRemanent { get; }
    }
}
