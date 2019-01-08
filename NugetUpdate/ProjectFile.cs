using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace NugetPackageUpdates
{
    public class ProjectFile
    {
        readonly byte[] _rawContent;
        private readonly bool _preserveBomChar;
        readonly XmlDocument _doc;

        public ProjectFile(string path, byte[] xmlContents, bool preserveBomChar = false)
        {
            _doc = new XmlDocument();
            _doc.PreserveWhitespace = true;
            ParseFileContents(xmlContents);
            _rawContent = xmlContents;
            _preserveBomChar = preserveBomChar;
            FilePath = path;
        }

        private void ParseFileContents(byte[] xmlContents)
        {
            using (var stream = new MemoryStream(xmlContents))
            {
                _doc.Load(stream);
            }
        }

        public string FilePath { get; }

        public void Reset()
        {
            ParseFileContents(_rawContent);
        }

        public IDictionary<string, string> ListPackages()
        {
            return _doc.SelectNodes($"Project/ItemGroup/PackageReference")
                .OfType<XmlNode>()
                .Where(x => x.Attributes != null 
                            && x.Attributes.OfType<XmlAttribute>().Any(y => string.Equals(y.Name, "Include", StringComparison.OrdinalIgnoreCase))
                            && x.Attributes.OfType<XmlAttribute>().Any(y => string.Equals(y.Name, "Version", StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(x => x.Attributes["Include"].Value, x => x.Attributes["Version"].Value);
        }

        public bool UpdatePackageReference(string packageName, string toVersion)
        {
            var node = _doc.SelectSingleNode($"Project/ItemGroup/PackageReference[@Include='{packageName}']");

            if (node != null)
            {
                node.Attributes["Version"].Value = toVersion;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            using (var sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                //Keep uft-8 BOM encoding (if required)
                if (_preserveBomChar &&
                    _rawContent[0] == 0xEF && _rawContent[1] == 0xBB && _rawContent[2] == 0xBF)
                {
                    sw.Write((char)65279);
                }

                sw.Write(_doc.OuterXml);

                return sw.ToString();
            }
        }

        public sealed class StringWriterWithEncoding : StringWriter
        {
            public override Encoding Encoding { get; }

            public StringWriterWithEncoding(Encoding encoding)
            {
                Encoding = encoding;
            }
        }
    }
}
