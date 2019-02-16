using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP;
using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfri.Extensions
{
    public static class CoapClientExtensions
    {
        public static Response SetValues(this CoapClient client, TradfriRequest request)
        {
            client.UriPath = request.UriPath;
            return client.Post(request.Payload);
        }

        public static Response GetValues(this CoapClient client, TradfriRequest request)
        {
            client.UriPath = request.UriPath;
            return client.Get();
        }

        public static Response UpdateValues(this CoapClient client, TradfriRequest request)
        {
            client.UriPath = request.UriPath;
            return client.Put(request.Payload);
        }
    }
}
