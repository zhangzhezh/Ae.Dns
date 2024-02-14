﻿using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Zone;
using System;
using System.Linq;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents metadata around a DNS resource record returned by a DNS server.
    /// </summary>
    public sealed class DnsResourceRecord : IEquatable<DnsResourceRecord>, IDnsByteArrayReader, IDnsByteArrayWriter, IDnsZoneConverter
    {
        /// <summary>
        /// The type of DNS query.
        /// </summary>
        public DnsQueryType Type { get; set; }
        /// <summary>
        /// The class of DNS query.
        /// </summary>
        public DnsQueryClass Class { get; set; }
        /// <summary>
        /// The time to live entry for this record, in seconds.
        /// </summary>
        public uint TimeToLive { get; set; }

        /// <summary>
        /// The host name associated with this record.
        /// </summary>
        public DnsLabels Host { get; set; }

        /// <summary>
        /// The value of this DNS record, which should be
        /// cast to the appropriate resource record type
        /// class depending on the <see cref="Type"/>.
        /// </summary>
        public IDnsResource? Resource { get; set; }

        private IDnsResource CreateResourceRecord(DnsQueryType recordType)
        {
            return recordType switch
            {
                DnsQueryType.A => new DnsIpAddressResource(),
                DnsQueryType.AAAA => new DnsIpAddressResource(),
                DnsQueryType.TEXT => new DnsTextResource(),
                DnsQueryType.CNAME => new DnsDomainResource(),
                DnsQueryType.NS => new DnsDomainResource(),
                DnsQueryType.PTR => new DnsDomainResource(),
                DnsQueryType.SPF => new DnsTextResource(),
                DnsQueryType.SOA => new DnsSoaResource(),
                DnsQueryType.MX => new DnsMxResource(),
                DnsQueryType.OPT => new DnsOptResource(),
                DnsQueryType.HTTPS => new DnsServiceBindingResource(),
                DnsQueryType.SVCB => new DnsServiceBindingResource(),
                _ => new DnsUnknownResource(),
            };
        }

        /// <inheritdoc/>
        public override string ToString() => $"Name: {Host} Type: {Type} Class: {Class} TTL: {TimeToLive} Resource: {Resource}";

        /// <inheritdoc/>
        public bool Equals(DnsResourceRecord? other)
        {
            if (other == null)
            {
                return false;
            }

            return Host.SequenceEqual(other.Host) &&
                Type == other.Type &&
                Class == other.Class &&
                TimeToLive == other.TimeToLive &&
                Equals(Resource, other.Resource);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DnsResourceRecord record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Host, Type, Class, TimeToLive, Resource);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset)
        {
            Host = DnsByteExtensions.ReadLabels(bytes, ref offset);
            Type = (DnsQueryType)DnsByteExtensions.ReadUInt16(bytes, ref offset);
            Class = (DnsQueryClass)DnsByteExtensions.ReadUInt16(bytes, ref offset);
            TimeToLive = DnsByteExtensions.ReadUInt32(bytes, ref offset);
            var dataLength = DnsByteExtensions.ReadUInt16(bytes, ref offset);
            if (dataLength > 0)
            {
                Resource = CreateResourceRecord(Type);
                FromBytesKnownLength(Resource, bytes, ref offset, dataLength);
            }
        }

        private static void FromBytesKnownLength(IDnsResource resource, ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            var expectedOffset = offset + length;
            resource.ReadBytes(bytes, ref offset, length);
            if (offset != expectedOffset)
            {
                throw new InvalidOperationException($"{resource.GetType().Name}.{nameof(IDnsResource.ReadBytes)} did not read to offset {expectedOffset} (read to {offset})");
            }
        }

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            DnsByteExtensions.ToBytes(Host.ToArray(), bytes, ref offset);
            DnsByteExtensions.ToBytes((ushort)Type, bytes, ref offset);
            DnsByteExtensions.ToBytes((ushort)Class, bytes, ref offset);
            DnsByteExtensions.ToBytes(TimeToLive, bytes, ref offset);

            // First, write the resource, but save two bytes for the size (and do not advance the offset)
            var resourceSize = 0;

            if (Resource != null)
            {
                Resource.WriteBytes(bytes.Slice(offset + sizeof(ushort)), ref resourceSize);
            }

            // Write the size of the resource in the two bytes preceeding (current offset)
            DnsByteExtensions.ToBytes((ushort)resourceSize, bytes, ref offset);

            // Advance the offset with the size of the resource
            offset += resourceSize;
        }

        /// <inheritdoc/>
        public string ToZone(IDnsZone zone)
        {
            return string.Join(" ", new object?[] { zone.ToFormattedHost(Host), TimeToLive, Class, Type, Resource?.ToZone(zone)}.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x?.ToString())));
        }

        /// <inheritdoc/>
        public void FromZone(IDnsZone zone, string input)
        {
            var parts = input.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

            var index = 0;

            if (char.IsWhiteSpace(input.First()))
            {
                Host = zone.Records.Last().Host;
            }
            else
            {
                Host = zone.FromFormattedHost(parts[index++]);
            }

            if (uint.TryParse(parts[index], out var ttl))
            {
                TimeToLive = ttl;
                index++;
            }
            else
            {
                TimeToLive = (uint)zone.DefaultTtl.TotalSeconds;
            }

            if (Enum.TryParse<DnsQueryClass>(parts[index], out var cl))
            {
                Class = cl;
                index++;
            }
            else
            {
                Class = zone.Records.Last().Class;
            }

            Type = (DnsQueryType)Enum.Parse(typeof(DnsQueryType), parts[index++]);
            Resource = CreateResourceRecord(Type);
            Resource.FromZone(zone, string.Join(" ", parts.Skip(index)));
        }
    }
}
