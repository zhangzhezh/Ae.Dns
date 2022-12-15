﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// Provides a <see cref="IDnsLookupSource"/> which parses DHCPD config files,
    /// reading the IP address and hostname, and adding it to the lookup table
    /// for DNS resolution.
    /// </summary>
    public sealed class DhcpdConfigDnsLookupSource : FileDnsLookupSource
    {
        private readonly string _hostnameSuffix;

        /// <summary>
        /// Construct a new <see cref="DhcpdLeasesDnsLookupSource"/> using the specified <see cref="FileInfo"/> and suffix string (for example, local).
        /// </summary>
        public DhcpdConfigDnsLookupSource(ILogger<DhcpdConfigDnsLookupSource> logger, FileInfo file, string hostnameSuffix = null) : base(logger, file)
        {
            _hostnameSuffix = hostnameSuffix;
            ReloadFile();
        }

        /// <inheritdoc/>
        protected override IEnumerable<(string hostname, IPAddress address)> LoadLookup(StreamReader sr)
        {
            string hostname = null;
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine().Trim();
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("host"))
                {
                    var parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

                    hostname = parts[1];
                }

                if (!string.IsNullOrWhiteSpace(hostname) && line.StartsWith("fixed-address"))
                {
                    var parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

                    var address = IPAddress.Parse(parts[1].Trim(new char[] { '"', ';' }));

                    yield return (_hostnameSuffix == null ? hostname : hostname + '.' + _hostnameSuffix, address);
                }
            }
        }
    }
}
