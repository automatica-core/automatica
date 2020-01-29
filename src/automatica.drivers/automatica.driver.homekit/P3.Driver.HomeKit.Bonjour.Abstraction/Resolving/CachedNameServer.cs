using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.HomeKit.Bonjour.Abstraction.Resolving
{
    /// <summary>
    ///   A caching name server.
    /// </summary>
    public class CachedNameServer : NameServer
    {
        /// <summary>
        ///   Removes any expired resource record from the cache.
        /// </summary>
        /// <param name="now">
        ///   The time to use to determine if a resource record is expired.
        ///   Defaults to <see cref="DateTime.Now"/>.
        /// </param>
        /// <remarks>
        ///   Authoritative nodes are not pruned.
        /// </remarks>
        public void Prune(DateTime? now = null)
        {
            now = now ?? DateTime.Now;

            var nodes = Catalog.Values.Where(node => !node.Authoritative);
            foreach (var node in nodes)
            {
                var resources = node.Resources.Where(r => r.IsExpired(now));
                foreach (var resource in resources)
                {
                    node.Resources.Remove(resource);
                }
            }
        }

        /// <summary>
        ///  Prune the cache in the background.
        /// </summary>
        /// <param name="interval">
        ///  The delay between pruning.
        /// </param>
        /// <returns>
        ///   Allows cancelation of the background task.
        /// </returns>
        /// <seealso cref="Prune"/>
        public CancellationTokenSource PruneContinuously(TimeSpan interval)
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    Prune();
                    await Task.Delay(interval, token);
                }
            }, token);

            return cts;
        }
    
        /// <summary>
        ///   Cache the response.
        /// </summary>
        /// <param name="response">
        ///   A response from a name server.
        /// </param>
        /// <remarks>
        ///   Both the <see cref="Message.Answers"/> and
        ///   the <see cref="Message.AdditionalRecords"/> are added to the cache.
        ///   Only resources records with a positive <see cref="ResourceRecord.TTL"/>
        ///   are added.
        /// </remarks>
        public void Add(Message response)
        {
            var resources = response
                .Answers.Concat(response.AdditionalRecords)
                .Where(r => r.TTL > TimeSpan.Zero);
            foreach (var resource in resources)
            {
                Catalog.Add(resource);
            }
        }
    }
}
