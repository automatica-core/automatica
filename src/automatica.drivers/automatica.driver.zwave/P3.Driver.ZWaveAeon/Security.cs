﻿using System;

namespace P3.Driver.ZWaveAeon
{
    [Flags()]
    public enum Security : byte
    {
        Nonde = 0x00,
        Security = 0x01,
        Controller = 0x02,
        SpecificDevice = 0x04,
        RoutingSlave = 0x08,
        BeamCapability = 0x10,
        Sensor250ms = 0x20,
        Sensor1000ms = 0x40,
        OptionalFunctionality = 0x80
    }
}
