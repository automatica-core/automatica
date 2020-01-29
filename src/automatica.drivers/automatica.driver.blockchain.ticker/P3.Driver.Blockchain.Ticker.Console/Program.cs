using System.Threading.Tasks;
using P3.Driver.Blockchain.Ticker.Driver.Bitcoin;

namespace P3.Driver.Blockchain.Ticker.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");

            var node = new BitcoinNode(null);
            node.AddNode(new BitcoinValueNode(null, "EUR"));
            node.AddNode(new BitcoinValueNode(null, "USD"));

            await node.Refresh();

            System.Console.ReadLine();
        }
    }
}
