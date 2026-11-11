using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Blockchain.Ticker.Driver;
using Xunit;

namespace P3.Driver.Blockchain.Ticker.Tests
{
    public sealed class BlockchainDriverTests : IDisposable
    {
        private static readonly Guid BtcNode = new("f728a20d-b805-4abd-9965-0e3c299a242c");
        private static readonly Guid BtcUsd = new("c2131e7a-501e-4216-b854-cba3d693103b");
        private static readonly Guid BtcUsdSymbol = new("1305e068-17e7-416e-9610-62a91b96e026");
        private static readonly Guid BtcEur = new("6b3975d0-631c-4537-9760-042a23aca312");
        private static readonly Guid BtcEurSymbol = new("5d552cfd-a4b9-4883-98eb-108f5e4de235");
        private static readonly Guid EthNode = new("f9541784-5107-4832-998e-049bec427ca1");
        private static readonly Guid EthUsd = new("8405e11c-a088-4709-9892-8c009683eeda");
        private static readonly Guid EthEurSymbol = new("0f90d2f5-672c-4746-9ec3-66060e88462c");
        private static readonly Guid AdaNode = new("99f6142d-81bc-471a-9339-5d3fd3012f47");
        private static readonly Guid AdaUsdSymbol = new("9856150b-fa97-4201-96b3-481a835b1370");

        private readonly HttpClient _originalClient;
        private readonly NodeTemplateFactoryMock _factory = new();
        private readonly BlockchainDriverFactory _driverFactory = new();
        private readonly CapturingDispatcher _dispatcher = new();

        public BlockchainDriverTests()
        {
            _originalClient = CoinNode.SharedClient;
            _driverFactory.InitNodeTemplates(_factory);
        }

        public void Dispose()
        {
            CoinNode.SharedClient = _originalClient;
        }

        private static string Quotation(string symbol, double price) =>
            $"{{\"Symbol\":\"{symbol}\",\"Name\":\"{symbol}\",\"Address\":\"0x0000000000000000000000000000000000000000\",\"Blockchain\":\"{symbol}\"," +
            $"\"Price\":{price.ToString(CultureInfo.InvariantCulture)},\"PriceYesterday\":1.5,\"VolumeYesterdayUSD\":2.5," +
            "\"Time\":\"2026-11-07T10:00:00Z\",\"Source\":\"diadata.org\",\"Signature\":\"0xabc\"}";

        private FakeHttpMessageHandler UseFakeApi(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new FakeHttpMessageHandler(responder);
            CoinNode.SharedClient = new HttpClient(handler);
            return handler;
        }

        private static HttpResponseMessage DefaultResponder(HttpRequestMessage request)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/Bitcoin/"))
                return FakeHttpMessageHandler.Json(Quotation("BTC", 65432.125));
            if (path.Contains("/Ethereum/"))
                return FakeHttpMessageHandler.Json(Quotation("ETH", 3210.5));
            if (path.Contains("/Cardano/"))
                return FakeHttpMessageHandler.Json(Quotation("ADA", 0.45));
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private NodeInstance Node(Guid template, params NodeInstance[] children)
        {
            var node = _factory.CreateNodeInstance(template);
            node.ObjId = Guid.NewGuid();
            foreach (var child in children)
            {
                node.InverseThis2ParentNodeInstanceNavigation.Add(child);
            }
            return node;
        }

        private async Task<Automatica.Core.Driver.IDriver> StartDriver(NodeInstance root, int pollSeconds = 7200)
        {
            root.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "poll").Value = pollSeconds;

            var context = new DriverContextMock(root, _driverFactory, _factory, _dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = _driverFactory.CreateDriver(context);
            await driver.Init();
            await driver.Configure();
            await driver.Start();
            return driver;
        }

        [Fact]
        public void InitNodeTemplates_CreatesDriverCoinAndValueTemplates()
        {
            Assert.NotNull(_factory.GetNodeTemplateById(_driverFactory.DriverGuid));
            Assert.Equal("blockchain-btc", _factory.GetNodeTemplateById(BtcNode).Key);
            Assert.Equal("blockchain-eth", _factory.GetNodeTemplateById(EthNode).Key);
            Assert.Equal("blockchain-ada", _factory.GetNodeTemplateById(AdaNode).Key);
            Assert.Equal("blockchain-btc-usd-with-symbol", _factory.GetNodeTemplateById(BtcUsdSymbol).Key);
            Assert.Equal("blockchain-ada-usd-with-symbol", _factory.GetNodeTemplateById(AdaUsdSymbol).Key);
        }

        [Fact]
        public async Task Start_DispatchesBitcoinPrices_WithAndWithoutSymbol()
        {
            var handler = UseFakeApi(DefaultResponder);
            var usd = Node(BtcUsd);
            var usdSymbol = Node(BtcUsdSymbol);
            var eur = Node(BtcEur);
            var eurSymbol = Node(BtcEurSymbol);
            var root = Node(_driverFactory.DriverGuid, Node(BtcNode, usd, usdSymbol, eur, eurSymbol));

            var driver = await StartDriver(root);
            try
            {
                Assert.Equal("65432.125", _dispatcher.LastValueOf(usd.ObjId));
                Assert.Equal("65432.125USD", _dispatcher.LastValueOf(usdSymbol.ObjId));
                Assert.Equal("65432.125", _dispatcher.LastValueOf(eur.ObjId));
                Assert.Equal("65432.125EUR", _dispatcher.LastValueOf(eurSymbol.ObjId));

                var uri = Assert.Single(handler.RequestedUris);
                Assert.Equal("https://api.diadata.org/v1/assetQuotation/Bitcoin/0x0000000000000000000000000000000000000000", uri.ToString());
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task Start_AllCoins_RequestOneUrlPerCoin()
        {
            var handler = UseFakeApi(DefaultResponder);
            var btc = Node(BtcUsd);
            var eth = Node(EthUsd);
            var ethEur = Node(EthEurSymbol);
            var ada = Node(AdaUsdSymbol);
            var root = Node(_driverFactory.DriverGuid, Node(BtcNode, btc), Node(EthNode, eth, ethEur), Node(AdaNode, ada));

            var driver = await StartDriver(root);
            try
            {
                Assert.Equal("65432.125", _dispatcher.LastValueOf(btc.ObjId));
                Assert.Equal("3210.5", _dispatcher.LastValueOf(eth.ObjId));
                Assert.Equal("3210.5EUR", _dispatcher.LastValueOf(ethEur.ObjId));
                Assert.Equal("0.45USD", _dispatcher.LastValueOf(ada.ObjId));

                var paths = handler.RequestedUris.Select(u => u.AbsolutePath).ToList();
                Assert.Equal(3, paths.Count);
                Assert.Contains(paths, p => p.StartsWith("/v1/assetQuotation/Bitcoin/"));
                Assert.Contains(paths, p => p.StartsWith("/v1/assetQuotation/Ethereum/"));
                Assert.Contains(paths, p => p.StartsWith("/v1/assetQuotation/Cardano/"));
                Assert.All(handler.RequestedUris, u => Assert.Equal("api.diadata.org", u.Host));
                // DIA answers 404 for an address that is not 20 bytes (Cardano had 39 zeros)
                Assert.All(paths, p => Assert.Matches("/0x[0-9a-fA-F]{40}$", p));
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task Start_GermanCulture_PriceUsesInvariantDecimalPoint()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-AT");
            try
            {
                UseFakeApi(DefaultResponder);
                var usd = Node(BtcUsd);
                var root = Node(_driverFactory.DriverGuid, Node(BtcNode, usd));

                var driver = await StartDriver(root);
                await driver.Stop();

                Assert.Equal("65432.125", _dispatcher.LastValueOf(usd.ObjId));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, "{}")]
        [InlineData(HttpStatusCode.TooManyRequests, "{}")]
        [InlineData(HttpStatusCode.OK, "null")]
        [InlineData(HttpStatusCode.OK, "this is not json")]
        public async Task Start_BadApiResponse_IsSwallowedAndNothingDispatched(HttpStatusCode status, string body)
        {
            UseFakeApi(_ => FakeHttpMessageHandler.Json(body, status));
            var usd = Node(BtcUsd);
            var root = Node(_driverFactory.DriverGuid, Node(BtcNode, usd));

            var driver = await StartDriver(root);
            await driver.Stop();

            Assert.Empty(_dispatcher.ValuesOf(usd.ObjId));
        }

        [Fact]
        public async Task Start_TransportFailure_IsSwallowed()
        {
            UseFakeApi(_ => throw new HttpRequestException("connection refused"));
            var usd = Node(BtcUsd);
            var root = Node(_driverFactory.DriverGuid, Node(BtcNode, usd));

            var driver = await StartDriver(root);
            await driver.Stop();

            Assert.Empty(_dispatcher.ValuesOf(usd.ObjId));
        }

        [Fact]
        public async Task ReadOnValueNode_RefreshesItsCoin()
        {
            var handler = UseFakeApi(DefaultResponder);
            var usd = Node(BtcUsd);
            var root = Node(_driverFactory.DriverGuid, Node(BtcNode, usd));

            var driver = await StartDriver(root);
            try
            {
                var valueNode = driver.Children.Single().Children.Single();
                await valueNode.Read();

                Assert.Equal(2, handler.RequestedUris.Count);
                Assert.Equal(2, _dispatcher.ValuesOf(usd.ObjId).Count);
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task ReadOnDriver_RefreshesAllCoins()
        {
            var handler = UseFakeApi(DefaultResponder);
            var root = Node(_driverFactory.DriverGuid, Node(BtcNode, Node(BtcUsd)), Node(EthNode, Node(EthUsd)));

            var driver = await StartDriver(root);
            try
            {
                await driver.Read();

                Assert.Equal(4, handler.RequestedUris.Count);
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task Timer_PollsAgainAfterThePollInterval()
        {
            var handler = UseFakeApi(DefaultResponder);
            var usd = Node(BtcUsd);
            var root = Node(_driverFactory.DriverGuid, Node(BtcNode, usd));

            var driver = await StartDriver(root, pollSeconds: 1);
            try
            {
                // one read on start, the timer adds the next one after ~1s
                Assert.NotNull(await _dispatcher.WaitForValue(usd.ObjId, count: 2, timeoutMs: 10000));
            }
            finally
            {
                await driver.Stop();
            }
        }
    }
}
