using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Rfc3339;

namespace PropertyList;

public class PlistWriter
{
    private static readonly Encoding UTF8NoBOM = new UTF8Encoding(false);

    private static XmlWriterSettings? _defaultXmlWriterSettings;
    private static XmlWriterSettings DefaultXmlWriterSettings => _defaultXmlWriterSettings ??= new()
    {
        Indent = true,
        Encoding = UTF8NoBOM,
    };

    public void Write(object plist, Stream stream)
    {
        using var xmlWriter = XmlWriter.Create(stream, DefaultXmlWriterSettings);
        Write(plist).WriteTo(xmlWriter);
    }

    public void Write(object plist, TextWriter writer)
    {
        using var xmlWriter = XmlWriter.Create(writer, DefaultXmlWriterSettings);
        Write(plist).WriteTo(xmlWriter);
    }

    public void Write(object plist, XmlWriter xmlWriter)
    {
        Write(plist).WriteTo(xmlWriter);
    }

    private XDocument Write(object plist)
    {
        var xDocument = new XDocument();
        xDocument.Add(new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null));
        var plistElement = new XElement(PlistElements.Plist, WriteNode(plist));
        plistElement.SetAttributeValue("version", "1.0");
        xDocument.Add(plistElement);
        return xDocument;
    }

    private XElement WriteNode(object node)
    {
        return node switch
        {
            IDictionary<string, object> dict => WriteDict(dict),
            IReadOnlyDictionary<string, object> dict => WriteDict(dict),
            ICollection array => WriteArray(array),
            IEnumerable<object> array => WriteArray(array),
            string s => new XElement(PlistElements.String, s),
            true => new XElement(PlistElements.True),
            false => new XElement(PlistElements.False),
            byte or sbyte or short or ushort or int or uint or long or ulong => new XElement(PlistElements.Integer, node),
            float or double or decimal => new XElement(PlistElements.Real, node),
            DateTime d => new XElement(PlistElements.Date, Rfc3339Formatter.Format(d)),
            DateTimeOffset d => new XElement(PlistElements.Date, Rfc3339Formatter.Format(d)),
            null => throw new ArgumentNullException(nameof(node), "null is not supported"),
            _ => throw new NotSupportedException($"Unknown node type {node.GetType()}")
        };
    }

    private XElement WriteArray(IEnumerable array)
    {
        return new XElement(PlistElements.Array, array.Cast<object>().Select(WriteNode));
    }

    private XElement WriteDict(IReadOnlyDictionary<string, object> dict) => WriteKeyValues(dict);
    private XElement WriteDict(IDictionary<string, object> dict) => WriteKeyValues(dict);
    private XElement WriteKeyValues(IEnumerable<KeyValuePair<string, object>> dict)
    {
        return new XElement(PlistElements.Dict, dict.SelectMany(WriteKeyValue));
    }

    private IEnumerable<XElement> WriteKeyValue(KeyValuePair<string, object> keyValue)
    {
        yield return new XElement(PlistElements.Key, keyValue.Key);
        yield return WriteNode(keyValue.Value);
    }
}
