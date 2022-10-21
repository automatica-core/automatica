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

        public override Version DriverVersion => new Version(0, 2, 1, 0);

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

            var btcGuid = new Guid("14e2dfca-40a3-4e0b-b44b-e52ef1ca2e00");
            factory.CreateInterfaceType(btcGuid, "BLOCKCHAIN_TICKER.BTC.NAME", "BLOCKCHAIN_TICKER.BTC.DESCRIPTION", int.MaxValue, 1, true);

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
    }
}
