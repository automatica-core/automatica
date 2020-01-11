using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.HomeKit.Bonjour.Abstraction.Resolving
{
    // DNSSEC aware name server.
    public partial class NameServer
    {
        /// <summary>
        ///   
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        async Task<Message> AddSecurityExtensionsAsync(Message request, Message response)
        {
            // If requestor doesn't do DNSSEC, then nothing more to do.
            if (!request.DO)
            {
                return response;
            }
            response.DO = true;

            await AddSecurityResourcesAsync(response.Answers);
            await AddSecurityResourcesAsync(response.AuthorityRecords);
            await AddSecurityResourcesAsync(response.AdditionalRecords);

            return response;
        }

        /// <summary>
        ///   Add the DNSSEC resources for the resource record set.
        /// </summary>
        /// <param name="rrset">
        ///   The set of resource records.
        /// </param>
        /// <remarks>
        ///   Add the signature records (RRSIG) for each resource in the set.
        /// </remarks>
        async Task AddSecurityResourcesAsync(List<ResourceRecord> rrset)
        {
            // Get the signature names and types that are needed.  Then
            // add the corresponding RRSIG records to the rrset.
            var neededSignatures = rrset
                .Where(r => r.CanonicalName != String.Empty) // ignore pseudo records as
                .GroupBy(r => new { r.CanonicalName, r.Type, r.Class })
                .Select(g => g.First());
            foreach (var need in neededSignatures)
            {
                var signatures = new Message();
                var question = new Question { Name = need.Name, Class = need.Class, Type = DnsType.RRSIG };
                if (!await FindAnswerAsync(question, signatures, CancellationToken.None))
                {
                    continue;
                }
                rrset.AddRange(signatures.Answers
                    .OfType<RRSIGRecord>()
                    .Where(r => r.TypeCovered == need.Type)
                    );
            }
        }
    }
}