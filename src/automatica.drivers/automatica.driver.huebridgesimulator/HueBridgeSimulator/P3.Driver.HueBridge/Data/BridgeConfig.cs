using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.HueBridge.Data
{
    public class BridgeSoftwareUpdate
    {
        public int UpdateState => 0;
        public bool CheckForUpdates => false;
        public object DeviceTypes => new object();
        public string Text => "";
        public bool Notify => false;
        public string Url => "";
    }

    public class BridgeConfig
    {
        public BridgeConfig()
        {
            SwUpdate = new BridgeSoftwareUpdate();
        }
        public bool PortalServices => true;
        public string Gateway { get; set; }
        public string Mac { get; set; }
        public string SwVersion => "9999999999";
        public string ApiVersion => "1.19.0";
        public bool LinkButton => false;

        public int ProxyPort => 0;

        public BridgeSoftwareUpdate SwUpdate { get; private set; }

        public string Netmask { get; set; }
        public string Name => "Automatica.Core Hue Bridge";
        public bool DHCP => true;
        public DateTime UTC => DateTime.UtcNow;
        public string ProxyAddress => "none";
        public DateTime LocalTime => DateTime.Now;
        public string TimeZone => System.TimeZoneInfo.Local.StandardName;
        public string ZigBeeChannel => "6";
        public string ModelId => "BSB002";
        public string BridgeId { get; set; }
        public bool FactoryNew => false;

        public Dictionary<string, HueUser> Whitelist => HueDriver.Instance.WhiteList;
    }
}
