using System;
using System.Collections.Generic;
using System.Linq;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.HomeConnect.Driver
{
    /// <summary>
    /// A data point of an appliance. <see cref="HomeConnectKey"/> is the Home Connect key the value is read from.
    /// </summary>
    internal sealed record HomeConnectDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, string HomeConnectKey, bool IsWriteable, bool DefaultCreated = true);

    internal static class HomeConnectDataPoints
    {
        public const string OperationState = "BSH.Common.Status.OperationState";
        public const string DoorState = "BSH.Common.Status.DoorState";
        public const string RemoteControlActive = "BSH.Common.Status.RemoteControlActive";
        public const string RemoteControlStartAllowed = "BSH.Common.Status.RemoteControlStartAllowed";
        public const string LocalControlActive = "BSH.Common.Status.LocalControlActive";
        public const string PowerState = "BSH.Common.Setting.PowerState";
        public const string ActiveProgram = "BSH.Common.Root.ActiveProgram";
        public const string SelectedProgram = "BSH.Common.Root.SelectedProgram";
        public const string RemainingProgramTime = "BSH.Common.Option.RemainingProgramTime";
        public const string ElapsedProgramTime = "BSH.Common.Option.ElapsedProgramTime";
        public const string ProgramProgress = "BSH.Common.Option.ProgramProgress";
        public const string PauseProgram = "BSH.Common.Command.PauseProgram";
        public const string ResumeProgram = "BSH.Common.Command.ResumeProgram";

        public const string PowerOn = "BSH.Common.EnumType.PowerState.On";
        public const string PowerOff = "BSH.Common.EnumType.PowerState.Off";
        public const string PowerStandby = "BSH.Common.EnumType.PowerState.Standby";

        public const string ConnectedKey = "connected";
        public const string RunningKey = "running";
        public const string PausedKey = "paused";
        public const string LastEventKey = "last_event";
        public const string CustomNumberKey = "custom_number";
        public const string CustomSwitchKey = "custom_switch";
        public const string CustomTextKey = "custom_text";

        public static readonly IReadOnlyList<HomeConnectDataPoint> All = new List<HomeConnectDataPoint>
        {
            new(new Guid("d9a5f1aa-e8ab-4553-a31c-40f38181104c"), ConnectedKey, "CONNECTED", NodeDataType.Boolean, null, false),
            new(new Guid("958f4064-dc79-4717-a26f-9aa7f5341188"), "operation_state", "OPERATION_STATE", NodeDataType.String, OperationState, false),
            new(new Guid("a6fb5c35-fbc4-4190-9614-2ac7e65641ba"), "door_state", "DOOR_STATE", NodeDataType.String, DoorState, false),
            new(new Guid("9384c9bf-12b6-4c95-ae4a-b452ce427281"), "remote_control_active", "REMOTE_CONTROL_ACTIVE", NodeDataType.Boolean, RemoteControlActive, false),
            new(new Guid("f699b507-7a08-4a87-bed1-0809e9fa334d"), "remote_start_allowed", "REMOTE_START_ALLOWED", NodeDataType.Boolean, RemoteControlStartAllowed, false),
            new(new Guid("830c6228-c50b-478a-8712-0e6b43b3d0f9"), "local_control_active", "LOCAL_CONTROL_ACTIVE", NodeDataType.Boolean, LocalControlActive, false),
            new(new Guid("7c1f0368-4839-4ff2-98c6-aecaf6e250bc"), "power", "POWER", NodeDataType.Boolean, PowerState, true),
            new(new Guid("a2b462e0-a06c-4aff-8dd1-86c63bf80e23"), "active_program", "ACTIVE_PROGRAM", NodeDataType.String, ActiveProgram, true),
            new(new Guid("1f1556c7-c81a-4e26-9209-0e743c7c34f9"), "selected_program", "SELECTED_PROGRAM", NodeDataType.String, SelectedProgram, true),
            new(new Guid("948fd11f-4e80-40e9-98d7-4c2c35215595"), RunningKey, "RUNNING", NodeDataType.Boolean, null, true),
            new(new Guid("566b7c54-5ccb-42ec-bfe1-392410b52e52"), PausedKey, "PAUSED", NodeDataType.Boolean, null, true),
            new(new Guid("d1728e08-360d-439d-bbf5-42ce9955d653"), "remaining_time", "REMAINING_TIME", NodeDataType.Integer, RemainingProgramTime, false),
            new(new Guid("075683a8-6f85-44e6-9061-0c233119c87b"), "elapsed_time", "ELAPSED_TIME", NodeDataType.Integer, ElapsedProgramTime, false),
            new(new Guid("b47bba9e-ef05-4aa0-b31d-e577e630eb0a"), "progress", "PROGRESS", NodeDataType.Integer, ProgramProgress, false),
            new(new Guid("303a35a8-ec08-4be8-afc4-5d4e582bee0d"), LastEventKey, "LAST_EVENT", NodeDataType.String, null, false),
            // any status, setting, option or command by its Home Connect key (property "key")
            new(new Guid("d0c36d63-c3ef-46db-91a1-17f95aa4a256"), CustomNumberKey, "CUSTOM_NUMBER", NodeDataType.Double, null, true, false),
            new(new Guid("869dce6a-944c-4968-9207-2ae39108b557"), CustomSwitchKey, "CUSTOM_SWITCH", NodeDataType.Boolean, null, true, false),
            new(new Guid("87484055-64e0-429f-9aaa-181c156e7da5"), CustomTextKey, "CUSTOM_TEXT", NodeDataType.String, null, true, false),
        };

        public static readonly IReadOnlyList<HomeConnectDataPoint> Defaults = All.Where(d => d.DefaultCreated).ToList();

        public static bool IsCustom(string key) => key is CustomNumberKey or CustomSwitchKey or CustomTextKey;
    }
}
