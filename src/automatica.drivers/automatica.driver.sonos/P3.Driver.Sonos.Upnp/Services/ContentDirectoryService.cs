using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using P3.Driver.Sonos.Upnp.Proxy;
using P3.Driver.Sonos.Upnp.Services.Factories;
using P3.Driver.Sonos.Upnp.Services.Models;

namespace P3.Driver.Sonos.Upnp.Services
{
    public class ContentDirectoryService : IContentDirectoryService
    {
        private readonly IUpnpClient _upnpClient;

        private const string ControlUrl = "/MediaServer/ContentDirectory/Control";
        private const string ActionNamespace = "urn:schemas-upnp-org:service:ContentDirectory:1";

        public ContentDirectoryService(string ipAddress)
        {
            var upnpUri = new SonosUri(ipAddress, ControlUrl);
            _upnpClient = new UpnpClient(upnpUri.ToUri(), ActionNamespace);
        }

        public async Task RefreshShareIndexAsync()
        {
            await _upnpClient.InvokeActionAsync("RefreshShareIndex");
        }

        public async Task<string> GetLastIndexChangeAsync()
        {
            return await _upnpClient.InvokeFuncAsync<string>("GetLastIndexChange");
        }

        public async Task<bool> GetShareIndexInProgressAsync()
        {
            var result = await _upnpClient.InvokeFuncAsync<int>("GetShareIndexInProgress");

            return result != 0;
        }
        
        public async Task<BrowseResponse> BrowseAsync(int startIndex = 0, int requestedCount = 100)
        {
            if(startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Invalid start index. Start index must be zero or greater.");
            if(requestedCount < 1)
                throw new ArgumentOutOfRangeException(nameof(requestedCount), "Invalid requested country. Requested count must be one or greater.");

            var xml = await _upnpClient.InvokeFuncWithResultAsync("Browse", new List<UpnpArgument>
            {
                new UpnpArgument("ObjectID", ObjectId.QueueAvtInstance0),                          // get queue
                //new UpnpArgument("ObjectID", ObjectId.Favourite + "2"),                         // get favorites
                //new UpnpArgument("ObjectID", ObjectId.AttributeArtist),                         // get artists
                new UpnpArgument("BrowseFlag", "BrowseDirectChildren"),         // BrowseDirectChildren || BrowseMetadata
                new UpnpArgument("Filter", ""),                             //"dc:title,res,dc:creator,upnp:artist,upnp:album")
                new UpnpArgument("StartingIndex", startIndex),
                new UpnpArgument("RequestedCount", requestedCount),
                new UpnpArgument("SortCriteria", "")
            });

            return new BrowseResponseFactory().CreateFor(ActionNamespace, xml);
        }
    }
}