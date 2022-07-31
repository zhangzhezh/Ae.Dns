﻿using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents an answer to a DNS query, generated by a DNS server.
    /// </summary>
    public sealed class DnsAnswer : IEquatable<DnsAnswer>, IDnsByteArrayReader, IDnsByteArrayWriter
    {
        /// <summary>
        /// The <see cref="DnsHeader"/> section of this answer.
        /// </summary>
        /// <value>Gets or sets the <see cref="DnsHeader"/>, which describes the original DNS query.</value>
        public DnsHeader Header { get; set; } = new DnsHeader();

        /// <summary>
        /// The list of DNS resources returned by the server.
        /// </summary>
        /// <value>Gets or sets the list representing <see cref="DnsResourceRecord"/> values returned by the DNS server.</value>
        public IList<DnsResourceRecord> Answers { get; set; } = new List<DnsResourceRecord>();

        /// <summary>
        /// The list of name server DNS resources returned by the server.
        /// </summary>
        /// <value>Gets or sets the list representing <see cref="DnsResourceRecord"/> values returned by the DNS server.</value>
        public IList<DnsResourceRecord> Nameservers { get; set; } = new List<DnsResourceRecord>();

        /// <summary>
        /// The list of additional DNS resources returned by the server.
        /// </summary>
        /// <value>Gets or sets the list representing <see cref="DnsResourceRecord"/> values returned by the DNS server.</value>
        public IList<DnsResourceRecord> Additional { get; set; } = new List<DnsResourceRecord>();

        /// <inheritdoc/>
        public bool Equals(DnsAnswer other) => Header.Equals(other.Header) && Answers.SequenceEqual(other.Answers);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsAnswer record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Header, Answers);

        private IList<DnsResourceRecord> ReadRecords(int count, byte[] bytes, ref int offset)
        {
            var records = new List<DnsResourceRecord>();
            for (var i = 0; i < count; i++)
            {
                records.Add(DnsByteExtensions.FromBytes<DnsResourceRecord>(bytes, ref offset));
            }

            return records;
        }

        /// <inheritdoc/>
        public void ReadBytes(byte[] bytes, ref int offset)
        {
            Header.ReadBytes(bytes, ref offset);
            Answers = ReadRecords(Header.AnswerRecordCount, bytes, ref offset);
            Nameservers = ReadRecords(Header.NameServerRecordCount, bytes, ref offset);
            Additional = ReadRecords(Header.AdditionalRecordCount, bytes, ref offset);
        }

        /// <inheritdoc/>
        public override string ToString() => $"RES: {Header.Id} Response: {Header.ResponseCode} Answers: {Answers.Count}" + string.Concat(Answers.Select(x => $"{Environment.NewLine} * {x}"));

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return DnsByteExtensions.ToBytes(Header);
            yield return Answers.Select(DnsByteExtensions.ToBytes).SelectMany(x => x);
            yield return Nameservers.Select(DnsByteExtensions.ToBytes).SelectMany(x => x);
            yield return Additional.Select(DnsByteExtensions.ToBytes).SelectMany(x => x);
        }
    }
}
