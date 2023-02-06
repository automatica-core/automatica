using System.Globalization;
using P3.PixooSharp.Assets;

namespace P3.Driver.Pixoo64.Screens
{
    public class CryptoPriceScreen : BaseScreen
    {
        public double? BitcoinPrice { get; set; }
        public double? EthereumPrice { get; set; }
        public double? CardanoPrice { get; set; }

        public string Currency { get; set; } = "$";

        public CryptoPriceScreen(IList<PixooSharp.Pixoo64> pixoo) : base(pixoo)
        {
            Title = "Crypto Prices";
        }

        protected override async Task PaintInternal()
        {
            await Task.CompletedTask;
            foreach (var pixoo in Pixoos)
            {
                pixoo.DrawText(5, 5, Palette.Red, Title);

                var textPos = 12;
                var pricePos = 20;
                if (BitcoinPrice.HasValue)
                {
                    pixoo.DrawText(5, textPos, Palette.White, "Bitcoin");
                    pixoo.DrawText(5, pricePos, Palette.White, Currency);
                    pixoo.DrawText(10, pricePos, Palette.White,
                        $"{BitcoinPrice.Value.ToString("n", CultureInfo.InvariantCulture)}");
                    textPos += 18;
                    pricePos += 18;
                }

                if (EthereumPrice.HasValue)
                {
                    pixoo.DrawText(5, textPos, Palette.White, "Ethereum");
                    pixoo.DrawText(5, pricePos, Palette.White, Currency);
                    pixoo.DrawText(10, pricePos, Palette.White,
                        $"{EthereumPrice.Value.ToString("n", CultureInfo.InvariantCulture)}");
                    textPos += 18;
                    pricePos += 18;
                }

                if (CardanoPrice.HasValue)
                {

                    pixoo.DrawText(5, textPos, Palette.White, "Cardano");
                    pixoo.DrawText(5, pricePos, Palette.White, Currency);
                    pixoo.DrawText(10, pricePos, Palette.White,
                        $"{CardanoPrice.Value.ToString("n", CultureInfo.InvariantCulture)}");
                }
            }
        }
    }
}
