using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace PropertyList
{
    public class PlistReader
    {
        public object Read(Stream stream)
        {
            return Read(new StreamReader(stream));
        }

        public object Read(TextReader reader)
        {
            using var xmlReader = XmlReader.Create(reader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, IgnoreWhitespace = true, IgnoreComments = true });
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.Name == "plist")
                        continue;
                    return ReadNode(xmlReader);
                }
            }

            throw new InvalidDataException("The file does not contain a node");
        }

        public object ReadNode(XmlReader xmlReader)
        {
            var name = xmlReader.LocalName;
            switch (name)
            {
                case "dict":
                    return ReadDict(xmlReader);
                case "array":
                    return ReadArray(xmlReader);
                case "string":
                    return xmlReader.Value;
                case "real":
                    return decimal.Parse(xmlReader.Value, CultureInfo.InvariantCulture);
                case "integer":
                    return long.Parse(xmlReader.Value, CultureInfo.InvariantCulture);
                case "date":
                    throw new NotImplementedException();
                case "true":
                    return true;
                case "false":
                    return false;
                default:
                    throw new InvalidDataException("Unknown {name} element");
            }
        }

        private object ReadArray(XmlReader xmlReader)
        {
            throw new NotImplementedException();
        }

        private object ReadDict(XmlReader xmlReader)
        {
            throw new NotImplementedException();
        }
    }
}