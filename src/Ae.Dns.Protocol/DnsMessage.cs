﻿using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents an answer to a DNS query, generated by a DNS server.
    /// </summary>
    public sealed class DnsMessage : IEquatable<DnsMessage>, IDnsByteArrayReader, IDnsByteArrayWriter
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
        public bool Equals(DnsMessage other) => Header.Equals(other.Header) &&
                                                Answers.SequenceEqual(other.Answers) &&
                                                Nameservers.SequenceEqual(other.Nameservers) &&
                                                Additional.SequenceEqual(other.Additional);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsMessage record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Header, Answers, Nameservers, Additional);

        private IList<DnsResourceRecord> ReadRecords(int count, ReadOnlySpan<byte> bytes, ref int offset)
        {
            var records = new DnsResourceRecord[count];
            for (var i = 0; i < count; i++)
            {
                records[i] = DnsByteExtensions.FromBytes<DnsResourceRecord>(bytes, ref offset);
            }

            return records;
        }

        private void EnsureCorrectCounts()
        {
            if (Header.AnswerRecordCount != Answers.Count)
            {
                throw new InvalidOperationException($"Header states there are {Header.AnswerRecordCount} answer records, there are {Answers.Count}");
            }

            if (Header.AdditionalRecordCount != Additional.Count)
            {
                throw new InvalidOperationException($"Header states there are {Header.AdditionalRecordCount} additional records, there are {Additional.Count}");
            }

            if (Header.NameServerRecordCount != Nameservers.Count)
            {
                throw new InvalidOperationException($"Header states there are {Header.NameServerRecordCount} nameserver records, there are {Nameservers.Count}");
            }
        }

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlySpan<byte> bytes, ref int offset)
        {
            EnsureCorrectCounts();
            Header.ReadBytes(bytes, ref offset);
            Answers = ReadRecords(Header.AnswerRecordCount, bytes, ref offset);
            Nameservers = ReadRecords(Header.NameServerRecordCount, bytes, ref offset);
            Additional = ReadRecords(Header.AdditionalRecordCount, bytes, ref offset);
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Header} Response: {Header.ResponseCode} Answers: {Answers.Count}" + string.Concat(Answers.Select(x => $"{Environment.NewLine} * {x}"));

        /// <inheritdoc/>
        public void WriteBytes(Span<byte> bytes, ref int offset)
        {
            EnsureCorrectCounts();
            DnsByteExtensions.ToBytes(Header, bytes, ref offset);

            foreach (var answer in Answers)
            {
                DnsByteExtensions.ToBytes(answer, bytes, ref offset);
            }

            foreach (var nameserver in Nameservers)
            {
                DnsByteExtensions.ToBytes(nameserver, bytes, ref offset);
            }

            foreach (var additional in Additional)
            {
                DnsByteExtensions.ToBytes(additional, bytes, ref offset);
            }
        }
    }
}
