using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;

namespace PropertyList;

[Flags]
internal enum PlistPropertyType
{
    Nil = 0x00,
    NullNil = 0x00,

    Boolean = 0x00,
    BooleanFalse = 0x08,
    BooleanTrue = 0x09,

    Url = 0x00,
    UrlBaseString = 0xC,
    UrlString = 0xD,

    Uuid = 0x00,
    UuidBytes = 0x0E,

    Padding = 0x00,
    PaddingFill = 0x0F,

    IntNumber = 0x10,
    RealNumber = 0x20,

    Date = 0x30,
    DateSize = 0x03,

    Data = 0x40,

    AsciiString = 0x50,
    UnicodeString = 0x60,
    Utf8String = 0x70,

    Uid = 0x80,

    Array = 0xA0,

    OrderedSet = 0xB0,

    Set = 0xC0,

    Dictionary = 0xD0,

    ExtendedSize = 0x0F,

    TypeMask = 0xF0,
    SizeMask = 0x0F,
}

partial class PlistReader
{
    private class BinaryPlist
    {
        public int OffsetTableOffsetSize;
        public int ObjectRefSize;
        public long NumObjects;
        public long TopObjectOffset;
        public long OffsetTableStart;
        public byte[] Offsets;

        public long GetOffset(int objectIndex) => Offsets.GetBigEndianInt(ObjectRefSize, objectIndex * ObjectRefSize);
    }

    private static DateTime DateStart = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public Dictionary<string, object> ReadBinary(Stream stream)
    {
        // https://en.wikipedia.org/wiki/Property_list
        // https://medium.com/@karaiskc/understanding-apples-binary-property-list-format-281e6da00dbd
        var trailer = new byte[32];
        stream.Seek(-32, SeekOrigin.End);
        stream.ReadAll(trailer);
        var numObjects = trailer.GetBigEndianInt(8, 8);
        var objectRefSize = trailer[6];
        var binaryPlist = new BinaryPlist
        {
            OffsetTableOffsetSize = trailer[5],
            ObjectRefSize = objectRefSize,
            NumObjects = numObjects,
            TopObjectOffset = trailer.GetBigEndianInt(8, 16),
            OffsetTableStart = trailer.GetBigEndianInt(8, 24),
            Offsets = new byte[numObjects * objectRefSize]
        };
        stream.Seek(binaryPlist.OffsetTableStart, SeekOrigin.Begin);
        stream.ReadAll(binaryPlist.Offsets);
        return (Dictionary<string, object>)ReadNode(stream, binaryPlist, 0)!;
    }

    private static object? ReadNode(Stream stream, BinaryPlist binaryPlist, int? objectIndex = null)
    {
        if (objectIndex.HasValue)
            stream.Seek(binaryPlist.GetOffset(objectIndex.Value), SeekOrigin.Begin);
        var fullCode = (PlistPropertyType)stream.ReadByte();
        var opCode = fullCode & PlistPropertyType.TypeMask;
        var size = fullCode & PlistPropertyType.SizeMask;
        return (opCode, size) switch
        {
            (PlistPropertyType.Nil, PlistPropertyType.NullNil) => null,
            (PlistPropertyType.Boolean, PlistPropertyType.BooleanFalse) => false,
            (PlistPropertyType.Boolean, PlistPropertyType.BooleanTrue) => true,
            (PlistPropertyType.Url, PlistPropertyType.UrlBaseString) => new Uri((string)ReadNode(stream, binaryPlist)!),
            (PlistPropertyType.Url, PlistPropertyType.UrlString) => new Uri((string)ReadNode(stream, binaryPlist)!),
            (PlistPropertyType.Uuid, PlistPropertyType.UuidBytes) => ReadUuid(stream),
            (PlistPropertyType.Padding, PlistPropertyType.PaddingFill) => null, //?
            (PlistPropertyType.IntNumber, _) => stream.ReadBigEndianInt(1 << (int)size),
            (PlistPropertyType.RealNumber, _) => ReadReal(stream, 1 << (int)size),
            (PlistPropertyType.Date, PlistPropertyType.DateSize) => DateStart + TimeSpan.FromSeconds(stream.ReadBigEndianInt(8)),
            (PlistPropertyType.Data, _) => stream.ReadBytes((int)ReadSize(stream, size)),
            (PlistPropertyType.AsciiString, _) => Encoding.ASCII.GetString(stream.ReadBytes((int)ReadSize(stream, size))),
            (PlistPropertyType.UnicodeString, _) => Encoding.BigEndianUnicode.GetString(stream.ReadBytes((int)ReadSize(stream, size) * 2)),
            (PlistPropertyType.Utf8String, _) => Encoding.UTF8.GetString(stream.ReadBytes((int)ReadSize(stream, size))),
            (PlistPropertyType.Array, _) => ReadArray(stream, (int)ReadSize(stream, size), binaryPlist),
            (PlistPropertyType.OrderedSet, _) => ReadArray(stream, (int)ReadSize(stream, size), binaryPlist),
            (PlistPropertyType.Set, _) => ReadArray(stream, (int)ReadSize(stream, size), binaryPlist),
            (PlistPropertyType.Dictionary, _) => ReadDictionary(stream, (int)ReadSize(stream, size), binaryPlist),
            _ => throw new InvalidDataException($"Unexpected byte {fullCode:X2}")
        };
    }

    private static IDictionary<string, object> ReadDictionary(Stream stream, int elementsCount, BinaryPlist binaryPlist)
    {
        var keysValues = ReadArray(stream, elementsCount * 2, binaryPlist);
        return Enumerable.Range(0, elementsCount).ToDictionary(i => (string)keysValues[i], i => keysValues[elementsCount + i]);
    }

    private static IList<object> ReadArray(Stream stream, int elementsCount, BinaryPlist binaryPlist)
    {
        return ReadElements(stream, elementsCount, binaryPlist).ToArray();
    }

    private static IEnumerable<object> ReadElements(Stream stream, int elementsCount, BinaryPlist binaryPlist)
    {
        var elementsIndexes = ReadElementsIndexes(stream, elementsCount, binaryPlist).ToArray();
        return elementsIndexes.Select(i => ReadNode(stream, binaryPlist, i));
    }

    private static IEnumerable<int> ReadElementsIndexes(Stream stream, int elementsCount, BinaryPlist binaryPlist)
    {
        for (int arrayIndex = 0; arrayIndex < elementsCount; arrayIndex++)
            yield return (int)stream.ReadBigEndianInt(binaryPlist.ObjectRefSize);
    }

    private static long ReadSize(Stream stream, PlistPropertyType size) => ReadSize(stream, (int)size);
    private static long ReadSize(Stream stream, int size)
    {
        if (size != (int)PlistPropertyType.ExtendedSize)
            return size;
        var extendedSizeSize = stream.ReadByte();
        if ((extendedSizeSize & 0xF0) != 0x10)
            throw new InvalidDataException("Incorrect extended size");
        var extendedSize = stream.ReadBigEndianInt(1 << (extendedSizeSize & 0x0F));
        return extendedSize;
    }

    private static double ReadReal(Stream stream, int size)
    {
        return size switch
        {
            4 => stream.ReadBigEndianFloat(),
            8 => stream.ReadBigEndianDouble(),
            _ => throw new InvalidDataException($"Unexpected size of {size} bytes")
        };
    }

    private static Guid ReadUuid(Stream stream)
    {
        var uuidBytes = new byte[16];
        stream.ReadAll(uuidBytes);
        return new Guid(uuidBytes);
    }
}
