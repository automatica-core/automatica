using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace P3.Driver.Pixoo64.Screens
{
    internal class CryptoScreenDriverNode : Pixoo64Screen<CryptoPriceScreen>
    {
        public CryptoScreenDriverNode(IDriverContext driverContext, IList<PixooSharp.Pixoo64> pixoo) : base(driverContext, pixoo)
        {
        }


        protected override CryptoPriceScreen CreateScreen()
        {
            return new CryptoPriceScreen(Pixoo, DriverContext.Logger);
        }

        protected override async Task SetScreenValue(object value, NodeInstance node, CancellationToken token = default)
        {
            await Task.CompletedTask;
            var dValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            switch (node.This2NodeTemplateNavigation.Key)
            {
                case "crypto-bitcoin":
                    Screen.BitcoinPrice = dValue;
                    break;
                case "crypto-ethereum":
                    Screen.EthereumPrice = dValue;
                    break;
                case "crypto-cardano":
                    Screen.CardanoPrice = dValue;
                    break;
            }
        }
    }
}
