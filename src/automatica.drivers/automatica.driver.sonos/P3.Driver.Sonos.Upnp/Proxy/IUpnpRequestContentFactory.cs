using System.Net.Http;
using P3.Driver.Sonos.Upnp.Proxy.Soap;

namespace P3.Driver.Sonos.Upnp.Proxy
{
    public interface IUpnpRequestContentFactory
    {
        StringContent CreateFor(SoapAction soapAction, UpnpArgumentList args);
    }
}