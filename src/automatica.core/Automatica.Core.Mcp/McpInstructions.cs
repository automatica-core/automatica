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

            Blinds with automatic shading: get_shading_status explains why a blind is up or down (sun, brightness,
            wind, rain, winter mode, manual moves); preview_shading simulates another time of day.

            Logs (only when the administrator allows it): list_log_files shows the files, newest first ("all*.log" holds
            every facility, the others one driver or part of the server each); read_log returns the last lines of a
            file, optionally only lines containing a text. Use them to explain errors or why a device did not react.
            get_log_levels / set_log_level show and change how much each facility logs; set it back when done.
            """;
    }
}
