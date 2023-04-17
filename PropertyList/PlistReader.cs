using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Rfc3339;

namespace PropertyList;

public partial class PlistReader
{
    public PlistFormat GetFormat(Stream stream, bool seekToBegin = true)
    {
        var header = new byte[8];
        stream.ReadAll(header);
        if (seekToBegin)
            stream.Seek(-header.Length, SeekOrigin.Current);
        // interesting stuff here https://stackoverflow.com/a/4522251/67004
        if (StartsWith(header, "bplist"))
            return PlistFormat.Binary;
        if (StartsWith(header, "<?xml"))
            return PlistFormat.Xml;
        return PlistFormat.Unknown;
    }

    private static bool StartsWith(byte[] header, string chars) => StartsWith(header, chars.Length, chars.Select(c => (byte)c));
    private static bool StartsWith(byte[] header, params char[] chars) => StartsWith(header, chars.Length, chars.Select(c => (byte)c));
    private static bool StartsWith(byte[] header, params byte[] chars) => StartsWith(header, chars.Length, chars);
    private static bool StartsWith(byte[] header, int length, IEnumerable<byte> chars) => header.Take(length).SequenceEqual(chars);

    public Dictionary<string, object> Read(Stream stream)
    {
        var seekableStream = stream.ToSeekableStream();
        return GetFormat(seekableStream) switch
        {
            PlistFormat.Unknown => throw new NotSupportedException("Unknown/unhandled plist format"),
            PlistFormat.Xml => ReadXml(seekableStream),
            PlistFormat.Binary => ReadBinary(seekableStream),
            _ => throw new ArgumentOutOfRangeException(nameof(stream), "The developer missed something here")
        };
    }
}