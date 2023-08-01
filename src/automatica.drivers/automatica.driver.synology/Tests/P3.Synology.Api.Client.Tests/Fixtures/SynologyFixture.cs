using System;
using P3.Synology.Api.Client.ApiDescription;

namespace P3.Synology.Api.Client.Tests.Fixtures
{
    public class SynologyFixture
    {
        public SynologyFixture()
        {
            ApisInfo = new DefaultApisInfo();
        }

        public IApisInfo ApisInfo { get; }

        public string BaseUrl => "http://dsm-url.com/webapi";

        public Uri BaseUri => new Uri(BaseUrl);

        public Uri GetBaseUriWithPath(string apiPath)
        {
            return new Uri(BaseUrl.TrimEnd('/') + "/" + apiPath.TrimStart('/'));
        }
    }
}
