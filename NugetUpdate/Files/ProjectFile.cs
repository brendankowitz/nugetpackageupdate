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

        public void Reset()
        {
            ParseFileContents(RawContent);
        }

        public IDictionary<string, string> ListPackages()
        {
            var packagesDictionary = _doc.SelectNodes($"Project/ItemGroup/PackageReference")
                ?.OfType<XmlNode>()
                ?.Where(x => x.Attributes != null
                            && x.Attributes.OfType<XmlAttribute>().Any(y => string.Equals(y.Name, "Include", StringComparison.OrdinalIgnoreCase))
                            && x.Attributes.OfType<XmlAttribute>().Any(y => string.Equals(y.Name, "Version", StringComparison.OrdinalIgnoreCase))
                            && !(x.Attributes["Version"] ?? x.Attributes["version"]).Value.StartsWith("$"))
                ?.ToDictionary(x => (x.Attributes["Include"] ?? x.Attributes["include"]).Value, x => (x.Attributes["Version"] ?? x.Attributes["version"]).Value);
            return packagesDictionary ?? new Dictionary<string, string>();
        }

        public bool UpdatePackageReference(string packageName, string toVersion)
        {
            try
            {
                var node = _doc.SelectSingleNode($"Project/ItemGroup/PackageReference[@Include='{packageName}']");

                if (node != null && node.Attributes != null)
                {
                    if ((node.Attributes["Version"] ?? node.Attributes["version"]) != null)
                    {
                        (node.Attributes["Version"] ?? node.Attributes["version"]).Value = toVersion;
                        return true;
                    }

                    var childVersion = (node.SelectSingleNode("Version") ?? node.SelectSingleNode("version"));
                    if (childVersion != null)
                    {
                        childVersion.InnerText = toVersion;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override string GetContent()
        {
            return _doc.OuterXml;
        }
    }
}