using Newtonsoft.Json;

namespace P3.Driver.HueBridge.Data
{
    public class HueLightState
    {
        private bool _on;
        private int _bri;

        public bool On
        {
            get => _on; set
            {
                _on = value;
                _bri = value ? 100 : 0;
            }
        }
        public int Bri { get => _bri;
            set
            {
                _bri = value;

                _on = value > 0;
            }
        }
        public string Alert => "none";
        public bool Reachable => true;
    }

    public class HueLight
    {
        public HueLight(string name, string uniqueId)
        {
            State = new HueLightState();
            State.On = false;

            Name = name;
            UniqueId = uniqueId;
        }
        [JsonIgnore]
        public int Id { get; internal set; }

        public HueLightState State { get; }

        public string Type => "Dimmable light";
        public string Name { get; }
        public string ModelId => "LWB007";
        public string ManufacturereName => "Philips";
        public string UniqueId { get; }
        public string SwVersion => "66012040";
    }
}
