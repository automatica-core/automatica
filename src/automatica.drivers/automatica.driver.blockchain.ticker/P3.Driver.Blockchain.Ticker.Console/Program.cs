using System.Threading.Tasks;
using P3.Driver.Blockchain.Ticker.Driver.Bitcoin;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;

namespace P3.Driver.Blockchain.Ticker.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var node = new BitcoinNode(null);
            node.AddNode(new BitcoinValueNode(null, "EUR", false, null));
            node.AddNode(new BitcoinValueNode(null, "USD", false, null));

            var ethNode = new EthereumNode(null);
            ethNode.AddNode(new EthereumValueNode(null, "ETH-EUR", false, "", ethNode));
            ethNode.AddNode(new EthereumValueNode(null, "ETH-USD", false, "", ethNode));

            await ethNode.Refresh();

            System.Console.ReadLine();
        }
    }
}
