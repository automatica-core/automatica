using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Pixoo64.Screens
{
    internal class CryptoScreenDriverNode : Pixoo64Screen<CryptoPriceScreen>
    {
        public CryptoScreenDriverNode(IDriverContext driverContext, PixooSharp.Pixoo64 pixoo) : base(driverContext, pixoo)
        {
        }


        protected override CryptoPriceScreen CreateScreen()
        {
            return new CryptoPriceScreen(Pixoo);
        }

        protected override async Task SetScreenValue(object value, NodeInstance node)
        {
            await Task.CompletedTask;
            var dValue = Convert.ToDouble(value);
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
