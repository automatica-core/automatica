using System;
using System.Collections.Generic;
using P3.Driver.Mammotion.Driver.Protocol;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Mammotion.Driver
{
    internal sealed record MammotionDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, MowerAction? Action = null)
    {
        public bool IsAction => Action.HasValue;
    }

    internal static class MammotionDataPoints
    {
        public static readonly MammotionDataPoint Online = new(new Guid("69f04798-b2ba-4bf6-8236-c2b1c7ce6940"), "online", "ONLINE", NodeDataType.Boolean);
        public static readonly MammotionDataPoint Battery = new(new Guid("5d286cc2-0559-4698-a6ce-f36ee3edd3cf"), "battery", "BATTERY", NodeDataType.Integer);
        public static readonly MammotionDataPoint DeviceState = new(new Guid("ad7fbeb2-d314-4cf9-878e-2699b03dcae0"), "device_state", "DEVICE_STATE", NodeDataType.Integer);
        public static readonly MammotionDataPoint DeviceStateText = new(new Guid("1a6015f3-320e-44c7-bd92-e7d70f4d368d"), "device_state_text", "DEVICE_STATE_TEXT", NodeDataType.String);
        public static readonly MammotionDataPoint ChargeState = new(new Guid("2a874567-3896-4a12-819c-4727309b231f"), "charge_state", "CHARGE_STATE", NodeDataType.Integer);
        public static readonly MammotionDataPoint Progress = new(new Guid("8022fa75-8202-4b78-a5b6-1f9a04df6230"), "mowing_progress", "MOWING_PROGRESS", NodeDataType.Integer);
        public static readonly MammotionDataPoint Area = new(new Guid("b3137cf4-2890-47bf-ae5c-a6c16563eec8"), "mowing_area", "MOWING_AREA", NodeDataType.Integer);
        public static readonly MammotionDataPoint RemainingTime = new(new Guid("b12b4ac4-5865-4505-8321-a04ae2b48e30"), "remaining_time", "REMAINING_TIME", NodeDataType.Integer);
        public static readonly MammotionDataPoint KnifeHeight = new(new Guid("7d4ef006-03a2-4368-b0a9-3260d24786f8"), "knife_height", "KNIFE_HEIGHT", NodeDataType.Integer);

        public static readonly IReadOnlyList<MammotionDataPoint> All = new[]
        {
            Online, Battery, DeviceState, DeviceStateText, ChargeState, Progress, Area, RemainingTime, KnifeHeight,
            new MammotionDataPoint(new Guid("00afb997-8a63-459e-9d6a-42282a573fd4"), "start", "START", NodeDataType.Boolean, MowerAction.Start),
            new MammotionDataPoint(new Guid("83086a57-7599-461c-96a7-27123d29b967"), "pause", "PAUSE", NodeDataType.Boolean, MowerAction.Pause),
            new MammotionDataPoint(new Guid("d46d3bce-d93f-4e0d-b42d-38f8b54ca554"), "resume", "RESUME", NodeDataType.Boolean, MowerAction.Resume),
            new MammotionDataPoint(new Guid("081ac808-85ed-4ed4-b954-502905cc4e26"), "stop", "STOP", NodeDataType.Boolean, MowerAction.Cancel),
            new MammotionDataPoint(new Guid("cff35e21-80a4-4c2c-a6bc-0133fa2e3641"), "return_to_dock", "RETURN_TO_DOCK", NodeDataType.Boolean, MowerAction.ReturnToDock),
        };
    }
}
