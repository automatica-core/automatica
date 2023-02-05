namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices.SG04LP3
{
    internal class SG04LP3DeviceMap : DeviceMap
    {
        public SG04LP3DeviceMap() : base(DeviceType.SG03LP3)
        {
            NameRegisterMap.Add("solarman-sg03lp3-pv1-power", new() {0x02A0});
            NameRegisterMap.Add("solarman-sg03lp3-pv2-power", new() { 0x02A1 });
            NameRegisterMap.Add("solarman-sg03lp3-pv1-voltage", new() { 0x02A4 });
            NameRegisterMap.Add("solarman-sg03lp3-pv2-voltage", new() { 0x02A6 });
            NameRegisterMap.Add("solarman-sg03lp3-pv1-current", new() { 0x02A5 });
            NameRegisterMap.Add("solarman-sg03lp3-pv2-current", new() { 0x02A7 });
            NameRegisterMap.Add("solarman-sg03lp3-daily-production", new() { 0x0211 });
            NameRegisterMap.Add("solarman-sg03lp3-total-production", new() { 0x0216, 0x217 });


            NameRegisterMap.Add("solarman-sg03lp3-total-battery-charge", new() { 0x0204, 0x0205 });
            NameRegisterMap.Add("solarman-sg03lp3-total-battery-discharge", new() { 0x0206, 0x0207 });
            NameRegisterMap.Add("solarman-sg03lp3-battery-power", new() { 0x024D });
            NameRegisterMap.Add("solarman-sg03lp3-battery-voltage", new() { 0x024B });
            NameRegisterMap.Add("solarman-sg03lp3-battery-soc", new() { 0x024C });
            NameRegisterMap.Add("solarman-sg03lp3-battery-current", new() { 0x024E });
            NameRegisterMap.Add("solarman-sg03lp3-battery-temperature", new() { 0x024A });

            NameRegisterMap.Add("solarman-sg03lp3-total-grid-power", new() { 0x0260 });
            NameRegisterMap.Add("solarman-sg03lp3-grid-voltage-l1", new() { 0x0256 });
            NameRegisterMap.Add("solarman-sg03lp3-grid-voltage-l2", new() { 0x0257 });
            NameRegisterMap.Add("solarman-sg03lp3-grid-voltage-l3", new() { 0x0258 });
            NameRegisterMap.Add("solarman-sg03lp3-internal-ct-l1-power", new() { 0x025C });
            NameRegisterMap.Add("solarman-sg03lp3-internal-ct-l2-power", new() { 0x025D });
            NameRegisterMap.Add("solarman-sg03lp3-internal-ct-l3-power", new() { 0x025E });
            NameRegisterMap.Add("solarman-sg03lp3-external-ct-l1-power", new() { 0x0268 });
            NameRegisterMap.Add("solarman-sg03lp3-external-ct-l2-power", new() { 0x0269 });
            NameRegisterMap.Add("solarman-sg03lp3-external-ct-l3-power", new() { 0x026A });
            NameRegisterMap.Add("solarman-sg03lp3-daily-energy-bought", new() { 0x0208 });
            NameRegisterMap.Add("solarman-sg03lp3-total-energy-bought", new() { 0x020A, 0x020B });
            NameRegisterMap.Add("solarman-sg03lp3-daily-energy-sold", new() { 0x0209 });
            NameRegisterMap.Add("solarman-sg03lp3-total-energy-sold", new() { 0x020C, 0x020D });
            NameRegisterMap.Add("solarman-sg03lp3-grid-status", new() { 0x0271 });


            NameRegisterMap.Add("solarman-sg03lp3-total-load-power", new() { 0x028D });
            NameRegisterMap.Add("solarman-sg03lp3-load-l1-power", new() { 0x028A });
            NameRegisterMap.Add("solarman-sg03lp3-load-l2-power", new() { 0x028B });
            NameRegisterMap.Add("solarman-sg03lp3-load-l3-power", new() { 0x028C });
            NameRegisterMap.Add("solarman-sg03lp3-load-voltage-l1", new() { 0x0284 });
            NameRegisterMap.Add("solarman-sg03lp3-load-voltage-l2", new() { 0x0285 });
            NameRegisterMap.Add("solarman-sg03lp3-load-voltage-l3", new() { 0x0286 });
            NameRegisterMap.Add("solarman-sg03lp3-daily-load-consumption", new() { 0x020E });
            NameRegisterMap.Add("solarman-sg03lp3-total-load-consumption", new() { 0x020F, 0x0210 });
            NameRegisterMap.Add("solarman-sg03lp3-smartload-enabled-status", new() { 0x00C3 });


            NameRegisterMap.Add("solarman-sg03lp3-running", new() { 0x01F4 });
            NameRegisterMap.Add("solarman-sg03lp3-total-power", new() { 0x00AF });
            NameRegisterMap.Add("solarman-sg03lp3-current-l1", new() { 0x0276 });
            NameRegisterMap.Add("solarman-sg03lp3-current-l2", new() { 0x0277 });
            NameRegisterMap.Add("solarman-sg03lp3-current-l3", new() { 0x0278 });
            NameRegisterMap.Add("solarman-sg03lp3-inverter-l1-power", new() { 0x0279 });
            NameRegisterMap.Add("solarman-sg03lp3-inverter-l2-power", new() { 0x027A });
            NameRegisterMap.Add("solarman-sg03lp3-inverter-l3-power", new() { 0x027B });
            NameRegisterMap.Add("solarman-sg03lp3-dc-temperature", new() { 0x021C });
            NameRegisterMap.Add("solarman-sg03lp3-ac-temperature", new() { 0x021D });
            NameRegisterMap.Add("solarman-sg03lp3-inverter-id", new() { 0x0003, 0x0004, 0x0005, 0x0006, 0x0007 });
            NameRegisterMap.Add("solarman-sg03lp3-communcation-board-version", new() { 0x0011 });
            NameRegisterMap.Add("solarman-sg03lp3-control-board-version", new() { 0x000D });
            NameRegisterMap.Add("solarman-sg03lp3-bootloader-version", new() { 0x000B });
            NameRegisterMap.Add("solarman-sg03lp3-grid-connected-status", new() { 0x0228 });


            NameRegisterMap.Add("solarman-sg03lp3-alarm-0", new() { 0x0229 });
            NameRegisterMap.Add("solarman-sg03lp3-alarm-1", new() { 0x022A });
            NameRegisterMap.Add("solarman-sg03lp3-alarm-2", new() { 0x022B });
            NameRegisterMap.Add("solarman-sg03lp3-alarm-3", new() { 0x022C });
            NameRegisterMap.Add("solarman-sg03lp3-alarm-4", new() { 0x022D });
            NameRegisterMap.Add("solarman-sg03lp3-alarm-5", new() { 0x022E });
        }
    }
}
