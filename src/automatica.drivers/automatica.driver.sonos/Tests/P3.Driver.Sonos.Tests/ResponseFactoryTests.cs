using System;
using System.Xml;
using System.Xml.Linq;
using P3.Driver.Sonos.Upnp.Services.Factories;
using P3.Driver.Sonos.Upnp.Services.Models;
using Xunit;
using static P3.Driver.Sonos.Tests.SoapResponses;

namespace P3.Driver.Sonos.Tests
{
    public class ResponseFactoryTests
    {
        private static readonly XNamespace AvNs = AvTransportNs;

        // ---------- GetTransportInfo ----------

        [Theory]
        [InlineData("PLAYING", TransportStateType.Playing, true)]
        [InlineData("STOPPED", TransportStateType.Stopped, false)]
        [InlineData("PAUSED_PLAYBACK", TransportStateType.Paused, false)]
        [InlineData("PAUSED_PLAYING", TransportStateType.Paused, false)]
        [InlineData("TRANSITIONING", TransportStateType.Transitioning, false)]
        public void GetTransportInfo_State_IsParsed(string state, TransportStateType expected, bool isPlaying)
        {
            var xml = Envelope($"<u:GetTransportInfoResponse xmlns:u=\"{AvTransportNs}\">" +
                               $"<CurrentTransportState>{state}</CurrentTransportState>" +
                               "<CurrentTransportStatus>OK</CurrentTransportStatus>" +
                               "<CurrentSpeed>1</CurrentSpeed></u:GetTransportInfoResponse>");

            var response = new GetTransportInfoResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal(expected, response.CurrentTransportState.Type);
            Assert.Equal(state, response.CurrentTransportState.Value);
            Assert.Equal(isPlaying, response.CurrentTransportState.IsPlaying);
            Assert.Equal("OK", response.CurrentTransportStatus);
            Assert.Equal("1", response.CurrentSpeed);
        }

        [Fact]
        public void GetTransportInfo_UnknownState_Throws()
        {
            var xml = Envelope($"<u:GetTransportInfoResponse xmlns:u=\"{AvTransportNs}\">" +
                               "<CurrentTransportState>NO_MEDIA_PRESENT</CurrentTransportState></u:GetTransportInfoResponse>");

            Assert.Throws<ArgumentException>(() => new GetTransportInfoResponseFactory().CreateFor(AvNs, xml));
        }

        [Fact]
        public void GetTransportInfo_WrongNamespace_Throws()
        {
            var xml = Envelope("<u:GetTransportInfoResponse xmlns:u=\"urn:other\">" +
                               "<CurrentTransportState>PLAYING</CurrentTransportState></u:GetTransportInfoResponse>");

            Assert.Throws<InvalidOperationException>(() => new GetTransportInfoResponseFactory().CreateFor(AvNs, xml));
        }

        [Fact]
        public void GetTransportInfo_Dtd_IsRejected()
        {
            var xml = Envelope($"<u:GetTransportInfoResponse xmlns:u=\"{AvTransportNs}\">" +
                               "<CurrentTransportState>PLAYING</CurrentTransportState>" +
                               "<CurrentTransportStatus>&xxe;</CurrentTransportStatus></u:GetTransportInfoResponse>", InternalEntityDoctype);

            Assert.Throws<XmlException>(() => new GetTransportInfoResponseFactory().CreateFor(AvNs, xml));
        }

        // ---------- GetTransportSettings ----------

        [Fact]
        public void GetTransportSettings_PlayMode_IsParsed()
        {
            var xml = Envelope($"<u:GetTransportSettingsResponse xmlns:u=\"{AvTransportNs}\">" +
                               "<PlayMode>SHUFFLE_NOREPEAT</PlayMode><RecQualityMode>NOT_IMPLEMENTED</RecQualityMode>" +
                               "</u:GetTransportSettingsResponse>");

            var response = new GetTransportSettingsResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal("SHUFFLE_NOREPEAT", response.PlayMode);
            Assert.Equal("NOT_IMPLEMENTED", response.RecQualityMode);
            Assert.Equal(PlayModeType.ShuffleNoRepeat, new PlayMode(response.PlayMode).Type);
        }

        [Fact]
        public void GetTransportSettings_MissingElements_AreNull()
        {
            var xml = Envelope($"<u:GetTransportSettingsResponse xmlns:u=\"{AvTransportNs}\"/>");

            var response = new GetTransportSettingsResponseFactory().CreateFor(AvNs, xml);

            Assert.Null(response.PlayMode);
            Assert.Null(response.RecQualityMode);
        }

        [Fact]
        public void GetTransportSettings_Dtd_IsRejected()
        {
            var xml = Envelope($"<u:GetTransportSettingsResponse xmlns:u=\"{AvTransportNs}\"><PlayMode>&xxe;</PlayMode>" +
                               "</u:GetTransportSettingsResponse>", InternalEntityDoctype);

            Assert.Throws<XmlException>(() => new GetTransportSettingsResponseFactory().CreateFor(AvNs, xml));
        }

        // ---------- GetMediaInfo ----------

        [Fact]
        public void GetMediaInfo_AllFields_AreParsed()
        {
            var xml = Envelope($"<u:GetMediaInfoResponse xmlns:u=\"{AvTransportNs}\">" +
                               "<NrTracks>12</NrTracks><MediaDuration>NOT_IMPLEMENTED</MediaDuration>" +
                               "<CurrentURI>x-rincon-queue:RINCON_000E58123456@01400#0</CurrentURI>" +
                               "<CurrentURIMetaData></CurrentURIMetaData><NextURI></NextURI><NextURIMetaData></NextURIMetaData>" +
                               "<PlayMedium>NETWORK</PlayMedium><RecordMedium>NOT_IMPLEMENTED</RecordMedium>" +
                               "<WriteStatus>NOT_IMPLEMENTED</WriteStatus></u:GetMediaInfoResponse>");

            var response = new GetMediaInfoResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal(12, response.NumberOfTracks);
            Assert.Equal("NOT_IMPLEMENTED", response.MediaDuration);
            Assert.Equal("x-rincon-queue:RINCON_000E58123456@01400#0", response.CurrentUri);
            Assert.Equal(string.Empty, response.CurrentUriMetaData);
            Assert.Equal("NETWORK", response.PlayMedium);
            Assert.Equal("NOT_IMPLEMENTED", response.RecordMedium);
            Assert.Equal("NOT_IMPLEMENTED", response.WriteStatus);
        }

        [Fact]
        public void GetMediaInfo_MissingTrackCount_IsZero()
        {
            var xml = Envelope($"<u:GetMediaInfoResponse xmlns:u=\"{AvTransportNs}\"><PlayMedium>NETWORK</PlayMedium></u:GetMediaInfoResponse>");

            var response = new GetMediaInfoResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal(0, response.NumberOfTracks);
            Assert.Null(response.CurrentUri);
        }

        [Fact]
        public void GetMediaInfo_Dtd_IsRejected()
        {
            var xml = Envelope($"<u:GetMediaInfoResponse xmlns:u=\"{AvTransportNs}\"><CurrentURI>&xxe;</CurrentURI></u:GetMediaInfoResponse>",
                InternalEntityDoctype);

            Assert.Throws<XmlException>(() => new GetMediaInfoResponseFactory().CreateFor(AvNs, xml));
        }

        // ---------- AddURIToQueue ----------

        [Fact]
        public void AddUriToQueue_Counts_AreParsed()
        {
            var xml = Envelope($"<u:AddURIToQueueResponse xmlns:u=\"{AvTransportNs}\">" +
                               "<FirstTrackNumberEnqueued>2</FirstTrackNumberEnqueued><NumTracksAdded>1</NumTracksAdded>" +
                               "<NewQueueLength>12</NewQueueLength></u:AddURIToQueueResponse>");

            var response = new AddUriToQueueResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal(2, response.FirstTrackNumberEnqueued);
            Assert.Equal(1, response.NumTracksAdded);
            Assert.Equal(12, response.NewQueueLength);
        }

        [Fact]
        public void AddUriToQueue_Dtd_IsRejected()
        {
            var xml = Envelope($"<u:AddURIToQueueResponse xmlns:u=\"{AvTransportNs}\"><NumTracksAdded>1</NumTracksAdded></u:AddURIToQueueResponse>",
                InternalEntityDoctype);

            Assert.Throws<XmlException>(() => new AddUriToQueueResponseFactory().CreateFor(AvNs, xml));
        }

        // ---------- GetPositionInfo ----------

        [Fact]
        public void GetPositionInfo_TrackAndMetadata_AreParsed()
        {
            var didl = Didl(TrackItem("Q:0/3", "Song &amp; Title", "The Artist", "The Album"));
            var xml = PositionInfo(Escape(didl));

            var response = new GetPositionInfoResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal(3, response.TrackNumber);
            Assert.Equal(new TimeSpan(0, 3, 25), response.TrackDuration);
            Assert.Equal(new TimeSpan(0, 1, 10), response.RelativeTime);
            Assert.Equal("NOT_IMPLEMENTED", response.AbsoluteTime);
            Assert.Equal(int.MaxValue, response.RelativeCount);
            Assert.Equal(int.MaxValue, response.AbsoluteCount);
            Assert.Equal("x-file-cifs://nas/music/t3.mp3", response.TrackUri);
            Assert.Equal(didl, response.TrackMetaDataRaw);

            var meta = response.TrackMetaData;
            Assert.Equal("Song & Title", meta.Title);
            Assert.Equal("The Artist", meta.Creator);
            Assert.Equal("The Album", meta.Album);
            Assert.Equal("object.item.audioItem.musicTrack", meta.Class);
            Assert.Equal("/getaa?s=1&u=x", meta.AlbumArtUri);
            Assert.Equal("0:03:25", meta.Duration);
            Assert.Equal("x-file-cifs:*:audio/mpeg:*", meta.ProtocolInfo);
            Assert.Equal("x-file-cifs://nas/music/Q:0/3.mp3", meta.Res);
        }

        [Theory]
        [InlineData("")]
        [InlineData("NOT_IMPLEMENTED")]
        public void GetPositionInfo_NoMetadata_ReturnsTvPlaceholder(string metadata)
        {
            var xml = PositionInfo(metadata);

            var response = new GetPositionInfoResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal("TV", response.TrackMetaData.Title);
            Assert.Equal("/Assets/Icons/tv.png", response.TrackMetaData.AlbumArtUri);
        }

        [Fact]
        public void GetPositionInfo_InvalidTimes_DefaultToZero()
        {
            var didl = Didl(TrackItem("Q:0/1", "t", "c", "a"));
            var xml = PositionInfo(Escape(didl), relTime: "NOT_IMPLEMENTED", trackDuration: "garbage");

            var response = new GetPositionInfoResponseFactory().CreateFor(AvNs, xml);

            Assert.Equal(TimeSpan.Zero, response.RelativeTime);
            Assert.Equal(TimeSpan.Zero, response.TrackDuration);
        }

        [Fact]
        public void GetPositionInfo_DtdInEnvelope_IsRejected()
        {
            var xml = PositionInfo("NOT_IMPLEMENTED", doctype: InternalEntityDoctype);

            Assert.Throws<XmlException>(() => new GetPositionInfoResponseFactory().CreateFor(AvNs, xml));
        }

        [Fact]
        public void GetPositionInfo_DtdInEmbeddedTrackMetadata_IsRejected()
        {
            // the DIDL inside TrackMetaData is parsed a second time, that parse must be safe too
            var didl = Didl(TrackItem("Q:0/3", "&xxe;", "c", "a"), "<!DOCTYPE DIDL-Lite [<!ENTITY xxe \"INJECTED\">]>");
            var xml = PositionInfo(Escape(didl));

            Assert.Throws<XmlException>(() => new GetPositionInfoResponseFactory().CreateFor(AvNs, xml));
        }

        // ---------- Browse ----------

        [Fact]
        public void Browse_Items_AreParsed()
        {
            var didl = Didl(TrackItem("Q:0/1", "First", "Artist A", "Album A", "0:02:00") +
                            TrackItem("Q:0/2", "Second", "Artist B", "Album B", "0:04:30"));
            var xml = Browse(Escape(didl), 2, 7, 42);

            var response = new BrowseResponseFactory().CreateFor(ContentDirectoryNs, xml);

            Assert.Equal(2, response.NumberReturned);
            Assert.Equal(7, response.TotalMatches);
            Assert.Equal(42, response.UpdateId);
            Assert.Equal(didl, response.DidlRaw);
            Assert.Equal(2, response.Items.Count);

            var first = response.Items[0];
            Assert.Equal("Q:0/1", first.Id);
            Assert.Equal("Q:0", first.ParentId);
            Assert.Equal("true", first.Restricted);
            Assert.Equal("First", first.Title);
            Assert.Equal("Artist A", first.Creator);
            Assert.Equal("Album A", first.Album);
            Assert.Equal(TimeSpan.FromMinutes(2), first.Duration);
            Assert.Equal("x-file-cifs:*:audio/mpeg:*", first.ProtocolInfo);
            Assert.Equal(ItemClassType.MusicTrack, first.ItemClassType);

            Assert.Equal(new TimeSpan(0, 4, 30), response.Items[1].Duration);
            Assert.Equal("Second", response.Items[1].Title);
        }

        [Fact]
        public void Browse_ZeroReturned_DoesNotParseItems()
        {
            var didl = Didl(TrackItem("Q:0/1", "First", "Artist A", "Album A"));
            var xml = Browse(Escape(didl), 0, 0, 1);

            var response = new BrowseResponseFactory().CreateFor(ContentDirectoryNs, xml);

            Assert.Empty(response.Items);
        }

        [Fact]
        public void Browse_DtdInEnvelope_IsRejected()
        {
            var xml = Browse(Escape(Didl("")), 0, 0, 1, InternalEntityDoctype);

            Assert.Throws<XmlException>(() => new BrowseResponseFactory().CreateFor(ContentDirectoryNs, xml));
        }

        [Fact]
        public void Browse_DtdInEmbeddedDidl_IsRejected()
        {
            var didl = Didl(TrackItem("Q:0/1", "&xxe;", "c", "a"), "<!DOCTYPE DIDL-Lite [<!ENTITY xxe \"INJECTED\">]>");
            var xml = Browse(Escape(didl), 1, 1, 1);

            Assert.Throws<XmlException>(() => new BrowseResponseFactory().CreateFor(ContentDirectoryNs, xml));
        }

        [Fact]
        public void Browse_FavoriteAndArtistClasses_MapToItemClassType()
        {
            var didl = Didl(
                "<item id=\"F:1\" parentID=\"F:\" restricted=\"true\"><res>x</res><upnp:class>object.itemobject.item.sonos-favorite</upnp:class></item>" +
                "<container id=\"A:ARTIST/x\" parentID=\"A:ARTIST\" restricted=\"true\"><res>y</res><upnp:class>object.container.person.musicArtist</upnp:class></container>" +
                "<item id=\"R:1\" parentID=\"R:\" restricted=\"true\"><res>z</res><upnp:class>object.item.audioItem.audioBroadcast</upnp:class></item>");
            var xml = Browse(Escape(didl), 3, 3, 1);

            var response = new BrowseResponseFactory().CreateFor(ContentDirectoryNs, xml);

            Assert.Equal(ItemClassType.Favorite, response.Items[0].ItemClassType);
            Assert.Equal(ItemClassType.MusicArtist, response.Items[1].ItemClassType);
            Assert.Equal(ItemClassType.Unknown, response.Items[2].ItemClassType);
        }
    }
}
