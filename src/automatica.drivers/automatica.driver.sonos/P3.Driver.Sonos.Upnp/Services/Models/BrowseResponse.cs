using System.Collections.Generic;

namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class BrowseResponse
    {
        private IList<Item> _items;

        public int NumberReturned { get; set; }

        public int TotalMatches { get; set; }

        public int UpdateId { get; set; }

        public string DidlRaw { get; set; }

        public IList<Item> Items
        {
            get => _items ?? (_items = new List<Item>());
            set => _items = value;
        }
    }
}