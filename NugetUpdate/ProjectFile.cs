using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace NugetPackageUpdates
{
    public class ProjectFile : File
    {
        readonly XmlDocument _doc;

        public ProjectFile(string path, byte[] contents, bool preserveBomChar = false)
            : base(path, contents, preserveBomChar)
        {
            _doc = new XmlDocument();
            _doc.PreserveWhitespace = true;
            ParseFileContents(contents);
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
            ParseFileContents(RawContent);
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

        protected override string GetContent()
        {
            return _doc.OuterXml;
        }
    }
}
