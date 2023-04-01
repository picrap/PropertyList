using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PropertyList;

public class PlistReader
{
    public Dictionary<string, object> Read(Stream stream)
    {
        return Read(XDocument.Load(stream));
    }

    public Dictionary<string, object> Read(TextReader reader)
    {
        return Read(XDocument.Load(reader));
    }

    public Dictionary<string, object> Read(XmlReader reader)
    {
        return Read(XDocument.Load(reader));
    }

    public Dictionary<string, object> Read(XDocument xDocument)
    {
        var plist = xDocument.Root;
        if (plist?.Name.LocalName != PlistElements.Plist)
            throw new InvalidDataException("Expected a plist");
        return (Dictionary<string, object>)ReadNode(plist.Elements().Single());
    }

    private object ReadNode(XElement node)
    {
        var name = node.Name.LocalName;
        return name switch
        {
            PlistElements.Dict => ReadDict(node),
            PlistElements.Array => ReadArray(node),
            PlistElements.String => node.Value,
            PlistElements.Real => decimal.Parse(node.Value, CultureInfo.InvariantCulture),
            PlistElements.Integer => long.Parse(node.Value, CultureInfo.InvariantCulture),
            PlistElements.Date => throw new NotImplementedException(),
            PlistElements.True => true,
            PlistElements.False => false,
            _ => throw new InvalidDataException("Unknown {name} element")
        };
    }

    private object ReadArray(XElement arrayElement)
    {
        return arrayElement.Elements().Select(ReadNode).ToList();
    }

    private object ReadDict(XElement dictElement)
    {
        return GetDictKeyValues(dictElement).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private IEnumerable<(string Key, object Value)> GetDictKeyValues(XElement dictElement)
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