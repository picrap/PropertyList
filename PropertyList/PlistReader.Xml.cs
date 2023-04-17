using Rfc3339;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System;
using System.Linq;
using System.Xml;

namespace PropertyList;

partial class PlistReader
{
    public Dictionary<string, object> ReadXml(Stream stream)
    {
        return ReadXml(XDocument.Load(stream));
    }

    public Dictionary<string, object> ReadXml(TextReader reader)
    {
        return ReadXml(XDocument.Load(reader));
    }

    public Dictionary<string, object> ReadXml(XmlReader reader)
    {
        return ReadXml(XDocument.Load(reader));
    }

    public Dictionary<string, object> ReadXml(XDocument xDocument)
    {
        var plist = xDocument.Root;
        if (plist?.Name.LocalName != PlistElements.Plist)
            throw new InvalidDataException("Expected a plist");
        return (Dictionary<string, object>)ReadNode(plist.Elements().Single());
    }

    private static object ReadNode(XElement node)
    {
        var name = node.Name.LocalName;
        return name switch
        {
            PlistElements.Dict => ReadDict(node),
            PlistElements.Array => ReadArray(node),
            PlistElements.String => node.Value,
            PlistElements.Real => decimal.Parse(node.Value, CultureInfo.InvariantCulture),
            PlistElements.Integer => long.Parse(node.Value, CultureInfo.InvariantCulture),
            PlistElements.Date => ReadDateTime(node),
            PlistElements.True => true,
            PlistElements.False => false,
            _ => throw new InvalidDataException("Unknown {name} element")
        };
    }

    private static DateTimeOffset ReadDateTime(XElement node)
    {
        if (!Rfc3339Parser.TryParse(node.Value, out Rfc3339DateTime dateTime))
            throw new InvalidDataException($"Can’t parse date '{node.Value}'");
        return dateTime.DateTimeOffset;
    }

    private static List<object> ReadArray(XElement arrayElement)
    {
        return arrayElement.Elements().Select(ReadNode).ToList();
    }

    private static IDictionary<string, object> ReadDict(XElement dictElement)
    {
        return GetDictKeyValues(dictElement).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static IEnumerable<(string Key, object Value)> GetDictKeyValues(XElement dictElement)
    {
        string? key = null;
        foreach (var element in dictElement.Elements())
            if (key is null)
            {
                if (element.Name.LocalName != PlistElements.Key)
                    throw new InvalidDataException($"Expected <{PlistElements.Key}> in dictionary");
                key = element.Value;
            }
            else
            {
                yield return (key, ReadNode(element));
                key = null;
            }
    }
}
