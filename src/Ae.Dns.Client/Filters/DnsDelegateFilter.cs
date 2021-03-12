﻿using System;
using Ae.Dns.Protocol;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// Provides the ability to use a delegate to deny DNS queries.
    /// </summary>
    public sealed class DnsDelegateFilter : IDnsFilter
    {
        private readonly Func<DnsHeader, bool> _shouldAllow;

        /// <summary>
        /// Create a new instance using the specified delegate to filter DNS queries.
        /// </summary>
        public DnsDelegateFilter(Func<DnsHeader, bool> shouldAllow) => _shouldAllow = shouldAllow;

        /// <inheritdoc/>
        public bool IsPermitted(DnsHeader query) => _shouldAllow(query);
    }
}
