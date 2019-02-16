﻿namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public enum AlarmDetailType : byte
    {
        None = 0,
        Intrusion = 1,
        IntrusionUnknownLocation = 2,
        TamperingProductCoveringRemoved = 3,
        TamperingInvalidCode = 4,
        GlassBreakage = 5,
        GlassBreakageUnknownLocation = 6,
        MotionDetection = 7,
        MotionDetectionUnknownLocation = 8,
    };
}
