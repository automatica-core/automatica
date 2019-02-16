﻿using System;
using P3.Driver.ZWaveAeon.Channel.Protocol;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class SensorAlarmReport : NodeReport
    {
        public readonly byte Source;
        public readonly AlarmType Type;
        public readonly byte Level;

        internal SensorAlarmReport(Node node, byte[] payload) : base(node)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (payload.Length < 3)
                throw new ReponseFormatException($"The response was not in the expected format. {GetType().Name}: Payload: {BitConverter.ToString(payload)}");

            // 5 bytes: byte 3 and 4 unknown
            Source = payload[0];
            Type = (AlarmType)payload[1];
            Level = payload[2];
        }

        public override string ToString()
        {
            return $"Source:{Source}, Type:{Type}, Level:{Level}";
        }
    }
}
