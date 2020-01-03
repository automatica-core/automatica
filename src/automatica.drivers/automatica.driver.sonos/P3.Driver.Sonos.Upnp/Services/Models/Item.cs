using System;

namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class Item
    {
        // (no NS)
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Restricted { get; set; }

        public string Res { get; set; }
        public TimeSpan Duration { get; set; }
        public string ProtocolInfo { get; set; }

        // upnp
        public string Class { get; set; }
        public string AlbumArtUri { get; set; }
        public string Album { get; set; }

        // dc
        public string Title { get; set; }
        public string Creator { get; set; }
        
        // r
        public string Ordinal { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string ResMd { get; set; }

        public ItemClassType ItemClassType
        {
            get
            {
                if(Class == ItemClass.MusicTrack)
                    return ItemClassType.MusicTrack;
                if(Class == ItemClass.Favorite)
                    return ItemClassType.Favorite;
                if(Class == ItemClass.Artist)
                    return ItemClassType.MusicArtist;

                return ItemClassType.Unknown;
            }
        }
    }

    public enum ItemClassType
    {
        Unknown,
        MusicTrack,
        MusicArtist,
        Favorite
    }

    public class ItemClass
    {
        public static readonly string Artist = "object.container.person.musicArtist";
        public static readonly string MusicTrack = "object.item.audioItem.musicTrack";
        public static readonly string Favorite = "object.itemobject.item.sonos-favorite";
        public static readonly string Stream = "object.item.audioItem.audioBroadcast";
    }
}