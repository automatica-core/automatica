using System;

namespace P3.Logic.Operations.MediaController
{
    /// <summary>
    /// The text on the command output of the media controller: "mode:command" or "mode:command:value", e.g. "2:volumeUp",
    /// "1:channel:12", "1:power:1"
    /// </summary>
    public static class MediaControllerCommand
    {
        public const string Power = "power";
        public const string VolumeUp = "volumeUp";
        public const string VolumeDown = "volumeDown";
        public const string Volume = "volume";
        public const string Mute = "mute";
        public const string ChannelUp = "channelUp";
        public const string ChannelDown = "channelDown";
        public const string Channel = "channel";
        public const string PlayPause = "playPause";
        public const string Next = "next";
        public const string Previous = "previous";
        public const string NavUp = "navUp";
        public const string NavDown = "navDown";
        public const string NavLeft = "navLeft";
        public const string NavRight = "navRight";
        public const string NavOk = "navOk";
        public const string Back = "back";
        public const string Menu = "menu";
        public const string Home = "home";

        public static string Format(int mode, string command, string value = null)
        {
            return value == null ? $"{mode}:{command}" : $"{mode}:{command}:{value}";
        }

        /// <summary>
        /// False for anything that is not "mode:command[:value]" with a mode 1..8
        /// </summary>
        public static bool TryParse(object text, out int mode, out string command, out string value)
        {
            mode = 0;
            command = null;
            value = null;

            var parts = Convert.ToString(text)?.Trim().Split(':', 3);
            if (parts == null || parts.Length < 2 || !int.TryParse(parts[0], out mode) || mode < 1 || mode > MediaControllerLogic.ModeCount)
            {
                return false;
            }

            command = parts[1].Trim();
            value = parts.Length == 3 ? parts[2].Trim() : null;
            return command.Length > 0;
        }
    }
}
