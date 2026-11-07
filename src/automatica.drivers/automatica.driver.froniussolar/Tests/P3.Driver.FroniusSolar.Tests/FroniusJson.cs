namespace P3.Driver.FroniusSolar.Tests
{
    internal static class FroniusJson
    {
        private const string Head =
            "\"Head\":{\"RequestArguments\":{\"DeviceClass\":\"Inverter\",\"DeviceId\":\"1\",\"Scope\":\"Device\"}," +
            "\"Status\":{\"Code\":0,\"Reason\":\"\",\"UserMessage\":\"\"},\"Timestamp\":\"2026-11-07T10:00:00+00:00\"}";

        private static string Unit(string unit, string value) => $"{{\"Unit\":\"{unit}\",\"Value\":{value}}}";

        public static string CommonInverterData() =>
            "{\"Body\":{\"Data\":{" +
            $"\"DAY_ENERGY\":{Unit("Wh", "8123.5")}," +
            "\"DeviceStatus\":{\"ErrorCode\":307,\"LEDColor\":2,\"LEDState\":0,\"MgmtTimerRemainingTime\":-1,\"StateToReset\":false,\"StatusCode\":7}," +
            $"\"FAC\":{Unit("Hz", "49.98")},\"IAC\":{Unit("A", "2.51")},\"IDC\":{Unit("A", "3.12")},\"PAC\":{Unit("W", "580")}," +
            $"\"TOTAL_ENERGY\":{Unit("Wh", "1000000")},\"UAC\":{Unit("V", "231.4")},\"UDC\":{Unit("V", "401.7")}," +
            $"\"YEAR_ENERGY\":{Unit("Wh", "500000")}" +
            "}}," + Head + "}";

        public static string P3InverterData() =>
            "{\"Body\":{\"Data\":{" +
            $"\"IAC_L1\":{Unit("A", "1.1")},\"IAC_L2\":{Unit("A", "1.2")},\"IAC_L3\":{Unit("A", "1.3")}," +
            $"\"UAC_L1\":{Unit("V", "230.1")},\"UAC_L2\":{Unit("V", "230.2")},\"UAC_L3\":{Unit("V", "230.3")}," +
            $"\"T_AMBIENT\":{Unit("C", "41")}," +
            // four different fan speeds, so a swapped field shows up
            $"\"ROTATION_SPEED_FAN_FL\":{Unit("RPM", "1100")},\"ROTATION_SPEED_FAN_FR\":{Unit("RPM", "1200")}," +
            $"\"ROTATION_SPEED_FAN_BL\":{Unit("RPM", "1300")},\"ROTATION_SPEED_FAN_BR\":{Unit("RPM", "1400")}" +
            "}}," + Head + "}";

        public static string PowerFlow(string inverterId = "1") =>
            "{\"Body\":{\"Data\":{" +
            $"\"Inverters\":{{\"{inverterId}\":{{\"DT\":102,\"E_Day\":8123.5,\"E_Total\":1000000,\"E_Year\":500000,\"P\":580}}}}," +
            "\"Site\":{\"E_Day\":8123.5,\"E_Total\":1000000,\"E_Year\":500000,\"Meter_Location\":\"unknown\",\"Mode\":\"produce-only\"," +
            "\"P_Akku\":null,\"P_Grid\":null,\"P_Load\":null,\"P_PV\":580,\"rel_Autonomy\":null,\"rel_SelfConsumption\":null}," +
            "\"Version\":\"12\"}}," + Head + "}";

        public static string NoBody() => "{" + Head + "}";

        public static string InverterInfo() =>
            "{\"Body\":{\"Data\":{\"1\":{\"DT\":102,\"PVPower\":5000,\"CustomName\":\"Roof\",\"UniqueID\":\"38183\",\"ErrorCode\":0,\"StatusCode\":7}," +
            "\"2\":{\"DT\":86,\"PVPower\":3000,\"CustomName\":\"Garage\",\"UniqueID\":\"40001\",\"ErrorCode\":102,\"StatusCode\":10}}}," + Head + "}";
    }
}
