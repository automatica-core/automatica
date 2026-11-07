using System;
using System.Collections.Generic;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Dreame.Driver
{
    internal enum DreameAccess
    {
        Read,
        ReadWrite,
        Action
    }

    /// <summary>
    /// A vacuum data point: a MIoT property (siid/piid) or action (siid/aiid). Mapping from Tasshack/dreame-vacuum (DreameVacuumPropertyMapping).
    /// </summary>
    internal sealed record DreameDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, int Siid, int Id, DreameAccess Access, Func<object, object> Map = null)
    {
        public bool IsProperty => Access != DreameAccess.Action && Siid > 0;
    }

    internal static class DreameDataPoints
    {
        public static readonly IReadOnlyDictionary<int, string> States = new Dictionary<int, string>
        {
            [1] = "sweeping", [2] = "idle", [3] = "paused", [4] = "error", [5] = "returning", [6] = "charging", [7] = "mopping", [8] = "drying",
            [9] = "washing", [10] = "returning_to_wash", [11] = "building", [12] = "sweeping_and_mopping", [13] = "charging_completed",
            [14] = "upgrading", [15] = "clean_summon", [27] = "spot_cleaning"
        };

        public static readonly IReadOnlyList<DreameDataPoint> All = new List<DreameDataPoint>
        {
            new(new Guid("59a22b7b-5ede-46d8-bb6a-44d78f2ce030"), "state", "STATE", NodeDataType.Integer, 2, 1, DreameAccess.Read),
            new(new Guid("117f0737-ca3a-4361-a58b-66aa36bb2bb0"), "state_text", "STATE_TEXT", NodeDataType.String, 2, 1, DreameAccess.Read,
                v => v is int i ? States.TryGetValue(i, out var s) ? s : $"state_{i}" : null),
            new(new Guid("b35a7207-7f3c-43dc-a781-ae6c1eb47b2a"), "error", "ERROR", NodeDataType.Integer, 2, 2, DreameAccess.Read),
            new(new Guid("e64804c0-bd52-4936-9542-d0330374ad35"), "battery", "BATTERY", NodeDataType.Integer, 3, 1, DreameAccess.Read),
            new(new Guid("d3dbfc90-4da7-4203-ab3f-4f0b6551afe9"), "charging_status", "CHARGING_STATUS", NodeDataType.Integer, 3, 2, DreameAccess.Read),
            new(new Guid("69596091-995a-4090-9d9d-2ae964840bd2"), "status", "STATUS", NodeDataType.Integer, 4, 1, DreameAccess.Read),
            new(new Guid("922ac9d7-557a-4cae-a282-b880edcef91f"), "cleaning_time", "CLEANING_TIME", NodeDataType.Integer, 4, 2, DreameAccess.Read),
            new(new Guid("35c367da-0db4-45bb-ab62-8861d7d8c1c5"), "cleaned_area", "CLEANED_AREA", NodeDataType.Integer, 4, 3, DreameAccess.Read),
            new(new Guid("0684be3c-88cd-4523-bea7-6b3fb0ac955a"), "suction_level", "SUCTION_LEVEL", NodeDataType.Integer, 4, 4, DreameAccess.ReadWrite),
            new(new Guid("7233a97d-3144-4fea-984b-d0523f606289"), "water_volume", "WATER_VOLUME", NodeDataType.Integer, 4, 5, DreameAccess.ReadWrite),
            new(new Guid("814c6b02-17dd-4cf1-b0f9-3faaad6d00bf"), "start", "START", NodeDataType.Boolean, 2, 1, DreameAccess.Action),
            new(new Guid("e428af3d-3785-4f62-b680-0693fb24b233"), "pause", "PAUSE", NodeDataType.Boolean, 2, 2, DreameAccess.Action),
            new(new Guid("9544234f-fa25-455a-878e-492ee110993b"), "stop", "STOP", NodeDataType.Boolean, 4, 2, DreameAccess.Action),
            new(new Guid("98427472-5d77-4036-8764-d08686fbe0b3"), "return_to_dock", "RETURN_TO_DOCK", NodeDataType.Boolean, 3, 1, DreameAccess.Action),
            new(new Guid("224f55bd-54a6-4801-a568-fc231964b290"), "locate", "LOCATE", NodeDataType.Boolean, 7, 1, DreameAccess.Action),
        };
    }
}
