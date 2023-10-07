using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Blockchain.Ticker.Driver
{
    public class BlockchainDriverFactory : DriverFactory
    {
        public override string DriverName => "BlockchainTicker";

        public override Guid DriverGuid => new Guid("7284093b-d1a0-4b9e-84ba-1f031f518af8");

        public override Version DriverVersion => new Version(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.blockchain.ticker";

        public override bool InDevelopmentMode => false;
        public override IDriver CreateDriver(IDriverContext config)
        {
            return new BlockchainDriver(config);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>9
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "BLOCKCHAIN_TICKER.NAME", "BLOCKCHAIN_TICKER.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "BLOCKCHAIN_TICKER.NAME", "BLOCKCHAIN_TICKER.DESCRIPTION", "blockchain_ticker", 
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);
          
            factory.CreatePropertyTemplate(new Guid("07453b89-d9df-450c-9208-7b08d317e2c0"), "BLOCKCHAIN_TICKER.POLL_INTERVAL.NAME", "BLOCKCHAIN_TICKER.POLL_INTERVAL.DESCRIPTION", "poll",
              PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(TimeSpan.FromSeconds(1).TotalSeconds, TimeSpan.FromHours(2).TotalSeconds), TimeSpan.FromMinutes(5).TotalSeconds, 1, 1);

            CreateBitcoin(factory);
            CreateEthereum(factory);
            CreateCardano(factory);
        }

        private void CreateBitcoin(INodeTemplateFactory factory)
        {
            var btcGuid = new Guid("14e2dfca-40a3-4e0b-b44b-e52ef1ca2e00");
            factory.CreateInterfaceType(btcGuid, "BLOCKCHAIN_TICKER.BTC.NAME", "BLOCKCHAIN_TICKER.BTC.DESCRIPTION", int.MaxValue, 1, false);

            factory.CreateNodeTemplate(new Guid("f728a20d-b805-4abd-9965-0e3c299a242c"), "BLOCKCHAIN_TICKER.BTC.NAME", "BLOCKCHAIN_TICKER.DESCRIPTION", "blockchain-btc", DriverGuid,
                btcGuid, true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreateNodeTemplate(new Guid("c2131e7a-501e-4216-b854-cba3d693103b"), "BLOCKCHAIN_TICKER.BTC.USD.NAME", "BLOCKCHAIN_TICKER.BTC.USD.DESCRIPTION", "blockchain-btc-usd", btcGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);
            factory.CreateNodeTemplate(new Guid("1305e068-17e7-416e-9610-62a91b96e026"), "BLOCKCHAIN_TICKER.BTC.USD_WITH_SYMBOL.NAME", "BLOCKCHAIN_TICKER.BTC.USD_WITH_SYMBOL.DESCRIPTION", "blockchain-btc-usd-with-symbol", btcGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);


            factory.CreateNodeTemplate(new Guid("6b3975d0-631c-4537-9760-042a23aca312"), "BLOCKCHAIN_TICKER.BTC.EUR.NAME", "BLOCKCHAIN_TICKER.BTC.EUR.DESCRIPTION", "blockchain-btc-eur", btcGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreateNodeTemplate(new Guid("5d552cfd-a4b9-4883-98eb-108f5e4de235"), "BLOCKCHAIN_TICKER.BTC.EUR_WITH_SYMBOL.NAME", "BLOCKCHAIN_TICKER.BTC.EUR_WITH_SYMBOL.DESCRIPTION", "blockchain-btc-eur-with-symbol", btcGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);
        }

        private void CreateEthereum(INodeTemplateFactory factory)
        {
            var ethGuid = new Guid("8d522b59-5d27-410f-8382-74ffcdbed8d3");
            factory.CreateInterfaceType(ethGuid, "BLOCKCHAIN_TICKER.BTC.NAME", "BLOCKCHAIN_TICKER.ETH.DESCRIPTION", int.MaxValue, 1, false);

            factory.CreateNodeTemplate(new Guid("f9541784-5107-4832-998e-049bec427ca1"), "BLOCKCHAIN_TICKER.ETH.NAME", "BLOCKCHAIN_TICKER.DESCRIPTION", "blockchain-eth", DriverGuid,
                ethGuid, true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreateNodeTemplate(new Guid("8405e11c-a088-4709-9892-8c009683eeda"), "BLOCKCHAIN_TICKER.ETH.USD.NAME", "BLOCKCHAIN_TICKER.ETH.USD.DESCRIPTION", "blockchain-eth-usd", ethGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);
            factory.CreateNodeTemplate(new Guid("a01a01a3-0fba-4538-8dc9-a891ae420d0a"), "BLOCKCHAIN_TICKER.ETH.USD_WITH_SYMBOL.NAME", "BLOCKCHAIN_TICKER.ETH.USD_WITH_SYMBOL.DESCRIPTION", "blockchain-eth-usd-with-symbol", ethGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);


            factory.CreateNodeTemplate(new Guid("4e587f00-33f9-4a70-af35-ea687aa7c087"), "BLOCKCHAIN_TICKER.ETH.EUR.NAME", "BLOCKCHAIN_TICKER.ETH.EUR.DESCRIPTION", "blockchain-eth-eur", ethGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreateNodeTemplate(new Guid("0f90d2f5-672c-4746-9ec3-66060e88462c"), "BLOCKCHAIN_TICKER.ETH.EUR_WITH_SYMBOL.NAME", "BLOCKCHAIN_TICKER.ETH.EUR_WITH_SYMBOL.DESCRIPTION", "blockchain-eth-eur-with-symbol", ethGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);
        }
        private void CreateCardano(INodeTemplateFactory factory)
        {
            var adaGuid = new Guid("05121a1a-bc8a-49e2-b8c5-b7c0bc00d1d8");
            factory.CreateInterfaceType(adaGuid, "BLOCKCHAIN_TICKER.BTC.NAME", "BLOCKCHAIN_TICKER.ADA.DESCRIPTION", int.MaxValue, 1, false);

            factory.CreateNodeTemplate(new Guid("99f6142d-81bc-471a-9339-5d3fd3012f47"), "BLOCKCHAIN_TICKER.ADA.NAME", "BLOCKCHAIN_TICKER.DESCRIPTION", "blockchain-ada", DriverGuid,
                adaGuid, true, true, true, false, true, NodeDataType.Date, 1, false);

            factory.CreateNodeTemplate(new Guid("f1c3ad00-2d37-41d0-8dd9-7050ea6931ac"), "BLOCKCHAIN_TICKER.ADA.USD.NAME", "BLOCKCHAIN_TICKER.ADA.USD.DESCRIPTION", "blockchain-ada-usd", adaGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);
            factory.CreateNodeTemplate(new Guid("9856150b-fa97-4201-96b3-481a835b1370"), "BLOCKCHAIN_TICKER.ADA.USD_WITH_SYMBOL.NAME", "BLOCKCHAIN_TICKER.ADA.USD_WITH_SYMBOL.DESCRIPTION", "blockchain-ada-usd-with-symbol", adaGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Date, 1, false);
        }
    }
}
