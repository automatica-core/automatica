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

            Building logic pages (administrators, or the API key with writing allowed): list_logic_templates and
            get_logic_template show the block types with the keys of their inputs, outputs and parameters;
            create_logic_page, add_logic (parameters by key) and link_logic (data point -> logic input, logic output ->
            data point or logic input) build a page like the logic editor; get_logic_page shows the result. Check the
            units a block expects (e.g. kW vs. W) against the data point values and put a math block in between when they
            differ. Tell the user what you changed; remove_link and remove_logic undo mistakes.

            Configuration (same permission): update_node_settings changes properties of a driver, device or data point by key
            (as get_datapoint lists them), e.g. the IP address of a driver whose device got a new address. The driver restarts;
            check read_log or get_current_values afterwards and tell the user the old and new values. Its history parameters
            switch the recording of a data point on or off (OnChange for counters and on/off states, Average for measurements).
            """;
    }
}
