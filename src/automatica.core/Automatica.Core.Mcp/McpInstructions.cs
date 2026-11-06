namespace Automatica.Core.Mcp
{
    internal static class McpInstructions
    {
        public const string Text = """
            This server is an Automatica.Core building automation server (KNX, Modbus, Sonos, sensors, ...).

            Data model:
            - The node tree holds the configured drivers and their devices. Leaf nodes that carry a value are "data points"
              (a light switch, a temperature, a meter reading). Every node has a GUID id.
            - Areas (building, floor, room) and categories (lights, heating, ...) group data points.
            - Logics are rules placed on logic pages; each logic has input and output interfaces with live values.
            - History: data points with trending enabled store their values over time (get_history); with the
              HyperSeries recorder enabled there are hourly/daily/... aggregates as well (get_aggregated_history).

            Workflow: use search_datapoints (by name, area or category) to find ids, then get_current_values,
            get_history or write_value. Before writing, check that the data point is writeable and confirm actions with
            a physical effect (lights, heating, blinds, doors) with the user. Timestamps are server local time unless
            they carry an offset.
            """;
    }
}
