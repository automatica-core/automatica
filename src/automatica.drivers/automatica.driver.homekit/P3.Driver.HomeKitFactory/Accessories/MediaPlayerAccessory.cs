using System;
using System.Threading.Tasks;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.Accessories
{
    /// <summary>
    /// A media player in HomeKit: a switch (on = playing, off = paused) for the Home app and Siri ("turn on the kitchen music"),
    /// plus a speaker service with mute and volume.
    /// </summary>
    internal sealed class MediaPlayerAccessory
    {
        private readonly MediaPlayerProxy _player;
        private readonly Action<Characteristic> _write;

        private MediaPlayerAccessory(Accessory accessory, MediaPlayerProxy player, Action<Characteristic> write)
        {
            Accessory = accessory;
            _player = player;
            _write = write;
            Playing = accessory.Specific.Characteristics[0];
            var speaker = accessory.Services[^1];
            Mute = speaker.GetOrAddCharacteristic(HapCharacteristics.Mute);
            Volume = speaker.GetOrAddCharacteristic(HapCharacteristics.Volume);
        }

        public Accessory Accessory { get; }
        public Characteristic Playing { get; }
        public Characteristic Mute { get; }
        public Characteristic Volume { get; }

        public static Accessory CreateAccessory(ulong aid, string name, string serial)
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(aid, name, "AutomaticaCore", serial, AccessoryFactory.SwitchType, HapCharacteristics.On);
            accessory.AddService(AccessoryFactory.SpeakerType, HapCharacteristics.Mute, HapCharacteristics.Volume);
            return accessory;
        }

        /// <summary>Takes the state of the player and keeps HomeKit up to date when it changes.</summary>
        public static MediaPlayerAccessory Bind(Accessory accessory, MediaPlayerProxy player, Action<Characteristic> write)
        {
            var binding = new MediaPlayerAccessory(accessory, player, write);
            binding.Update(false);
            player.RegisterValueCallback(() => binding.Update(true));
            return binding;
        }

        private void Update(bool notify)
        {
            Set(Playing, _player.IsPlaying, notify);
            Set(Mute, _player.IsMuted, notify);
            Set(Volume, Math.Clamp(_player.Volume, 0, 100), notify);
        }

        private void Set(Characteristic characteristic, object value, bool notify)
        {
            if (Equals(characteristic.Value, value))
            {
                return;
            }

            characteristic.Value = value;
            if (notify)
            {
                _write(characteristic);
            }
        }

        /// <summary>A write from a HomeKit controller, HomeKit may send booleans as 0/1 and numbers as any number type.</summary>
        public Task HandleWriteAsync(Characteristic characteristic, object value)
        {
            if (characteristic == Playing)
            {
                return ToBool(value) ? _player.PlayAsync() : _player.PauseAsync();
            }

            if (characteristic == Mute)
            {
                return _player.SetMuteAsync(ToBool(value));
            }

            if (characteristic == Volume)
            {
                return _player.SetVolumeAsync(Math.Clamp(Convert.ToInt32(value), 0, 100));
            }

            return Task.CompletedTask;
        }

        private static bool ToBool(object value) => value is bool b ? b : Convert.ToDouble(value) != 0;
    }
}
