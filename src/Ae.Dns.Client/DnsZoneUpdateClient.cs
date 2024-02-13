﻿using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Accepts messages with <see cref="DnsOperationCode.UPDATE"/>, and stores the record for use elsewhere.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsZoneUpdateClient : IDnsClient
    {
        private readonly ILogger<DnsZoneUpdateClient> _logger;
        private readonly IDnsZone _dnsZone;

        /// <summary>
        /// Create the new <see cref="DnsZoneUpdateClient"/> using the specified <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dnsZone"></param>
        public DnsZoneUpdateClient(ILogger<DnsZoneUpdateClient> logger, IDnsZone dnsZone)
        {
            _logger = logger;
            _dnsZone = dnsZone;
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            query.EnsureOperationCode(DnsOperationCode.UPDATE);
            query.EnsureQueryType(DnsQueryType.SOA);
            query.EnsureHost(_dnsZone.Origin);

            _logger.LogInformation("Recieved update query with pre-reqs {PreReqs} and update type {UpdateType}", query.GetZoneUpdatePreRequisite(), query.GetZoneUpdateType());

            // TODO: this logic is bad
            var hostnames = query.Nameservers.Select(x => x.Host.ToString()).ToArray();
            var addresses = query.Nameservers.Select(x => x.Resource).OfType<DnsIpAddressResource>().Select(x => x.IPAddress).ToArray();

            void ChangeRecords(ICollection<DnsResourceRecord> records)
            {
                foreach (var recordToRemove in records.Where(x => hostnames.Contains(x.Host.ToString())).ToArray())
                {
                    records.Remove(recordToRemove);
                }

                foreach (var recordToRemove in records.Where(x => x.Resource is DnsIpAddressResource ipr && addresses.Contains(ipr.IPAddress)).ToArray())
                {
                    records.Remove(recordToRemove);
                }

                foreach (var nameserver in query.Nameservers)
                {
                    records.Add(nameserver);
                }
            };

            if (query.Nameservers.Count > 0 && hostnames.All(x => !Regex.IsMatch(x, @"\s")) && hostnames.All(x => x.ToString().EndsWith(_dnsZone.Origin)))
            {
                await _dnsZone.Update(ChangeRecords);
                return query.CreateAnswerMessage(DnsResponseCode.NoError, ToString());
            }

            return query.CreateAnswerMessage(DnsResponseCode.Refused, ToString());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(DnsZoneUpdateClient)}({_dnsZone})";
    }
}