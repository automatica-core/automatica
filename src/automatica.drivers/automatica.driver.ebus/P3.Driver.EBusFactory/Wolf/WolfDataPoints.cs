using System;
using System.Collections.Generic;
using P3.Driver.EBus;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.EBusFactory.Wolf
{
    internal enum WolfSource
    {
        /// <summary>polled with a Wolf register read (50 22)</summary>
        Register,
        /// <summary>from the heat generator's operational data broadcast (05 03)</summary>
        HeatGeneratorStatus,
        /// <summary>from the heat generator's fault code broadcast (FE 01)</summary>
        FaultCode
    }

    /// <param name="Register">low word for 32 bit values</param>
    /// <param name="HighRegister">register with the high word of a 32 bit value</param>
    internal sealed record WolfDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, WolfSource Source,
        ushort Register = 0, ushort? HighRegister = null, double Scale = 1, bool Signed = false, Func<HeatGeneratorStatus, object> FromStatus = null)
    {
        public object Convert(ushort low, ushort? high = null)
        {
            double value = HighRegister.HasValue ? (high ?? 0) * 65536.0 + low : Signed ? (short)low : low;
            value *= Scale;
            return Type == NodeDataType.Integer ? (int)Math.Round(value) : Math.Round(value, 3);
        }
    }

    /// <summary>
    /// Wolf BWL-1 air/water heat pump. The registers were read from a BWL-1 and matched with the values it broadcasts
    /// (flow, return, hot water, outside). "Unverified" ones fit the documentation of other Wolf heat pumps and the
    /// read values, but were not confirmed with the display of the heat pump.
    /// </summary>
    internal static class WolfBwl1
    {
        public const string Key = "wolf-bwl1";
        public const string CustomRegisterKey = "wolf-register";
        public const byte DefaultAddress = 0x08;

        public static readonly Guid Interface = new("f070ee43-f6d9-4e3a-ab60-b9f80dcff2a1");
        public static readonly Guid Template = new("3c54eccf-a956-4d06-8b23-32a85d78d188");
        public static readonly Guid CustomRegisterTemplate = new("cd2efa79-a1ae-48d4-9671-de3cf5b516e8");

        public static readonly IReadOnlyList<WolfDataPoint> DataPoints = new[]
        {
            new WolfDataPoint(new Guid("8b25d23a-2eaf-4b82-aa0b-acd41fa4598b"), "flow-temperature", "FLOW_TEMPERATURE", NodeDataType.Double, WolfSource.Register, 13, Scale: 0.01, Signed: true),
            new WolfDataPoint(new Guid("38f584ff-8f64-4766-b7a0-e8422a6251c4"), "return-temperature", "RETURN_TEMPERATURE", NodeDataType.Double, WolfSource.Register, 22, Scale: 0.01, Signed: true),
            new WolfDataPoint(new Guid("4ba8c906-026b-41a7-8494-2e03eff355b4"), "flow-setpoint", "FLOW_SETPOINT", NodeDataType.Double, WolfSource.Register, 2, Scale: 0.1, Signed: true),
            new WolfDataPoint(new Guid("ea8e6a49-8f3e-4bd1-9696-e64ae921cf0f"), "hot-water-temperature", "HOT_WATER_TEMPERATURE", NodeDataType.Double, WolfSource.Register, 14, Scale: 0.1, Signed: true),
            new WolfDataPoint(new Guid("4c692aea-a3ce-4da4-9a13-bc8ee3ce1a7e"), "hot-water-setpoint", "HOT_WATER_SETPOINT", NodeDataType.Double, WolfSource.Register, 19, Scale: 0.1, Signed: true),
            new WolfDataPoint(new Guid("52fed175-1ac0-4b8e-80b0-6ac4b59a63f2"), "outside-temperature", "OUTSIDE_TEMPERATURE", NodeDataType.Double, WolfSource.Register, 12, Scale: 0.1, Signed: true),
            new WolfDataPoint(new Guid("0f99e117-ae48-4faf-a839-7e5fd780917d"), "circuit-2-flow-temperature", "CIRCUIT_2_FLOW_TEMPERATURE", NodeDataType.Double, WolfSource.Register, 15, Scale: 0.1, Signed: true),
            new WolfDataPoint(new Guid("ce5038a7-4110-4a3f-b195-8d63e9cf5656"), "compressor-hours", "COMPRESSOR_HOURS", NodeDataType.Integer, WolfSource.Register, 554),
            new WolfDataPoint(new Guid("99e6b487-aa5e-4e6f-a38f-9992fe538dc0"), "compressor-starts", "COMPRESSOR_STARTS", NodeDataType.Integer, WolfSource.Register, 550, HighRegister: 551),
            new WolfDataPoint(new Guid("8dd89ffd-0ceb-4753-bb10-697dc488154f"), "running", "RUNNING", NodeDataType.Boolean, WolfSource.HeatGeneratorStatus, FromStatus: s => s.Running),
            new WolfDataPoint(new Guid("bcc2dcd1-f1ec-49a9-b4a5-900a1233437f"), "circulation-pump", "CIRCULATION_PUMP", NodeDataType.Boolean, WolfSource.HeatGeneratorStatus, FromStatus: s => s.CirculationPump),
            new WolfDataPoint(new Guid("3bfbfad4-5672-405f-a9e5-cc229d2ac220"), "fault-code", "FAULT_CODE", NodeDataType.Integer, WolfSource.FaultCode),
        };
    }
}
