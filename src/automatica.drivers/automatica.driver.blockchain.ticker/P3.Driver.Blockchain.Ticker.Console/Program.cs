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
            node.AddNode(new BitcoinValueNode(null, "EUR", false, null));
            node.AddNode(new BitcoinValueNode(null, "USD", false, null));

            await node.Refresh();

            System.Console.ReadLine();
        }
    }
}
