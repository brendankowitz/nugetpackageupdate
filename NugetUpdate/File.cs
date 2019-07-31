using System;
using System.IO;
using System.Text;

namespace NugetPackageUpdates
{
    public abstract class File
    {
        protected readonly byte[] RawContent;
        protected readonly bool PreserveBomChar;
        private static readonly char bomChar = (char)65279;


        protected File(string path, byte[] contents, bool preserveBomChar = false)
        {
            RawContent = contents;
            PreserveBomChar = preserveBomChar;
            FilePath = path;
        }

        public string FilePath { get; }

        protected abstract string GetContent();

        public override string ToString()
        {
            using (var sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                //Keep uft-8 BOM encoding (if required)
                if (PreserveBomChar &&
                    RawContent[0] == 0xEF && RawContent[1] == 0xBB && RawContent[2] == 0xBF)
                {
                    sw.Write(bomChar);
                }

                sw.Write(GetContent());

                return sw.ToString();
            }
        }

        public Change ToChange()
        {
            var fileContents = ToString();

            var trim = ToString().Trim(bomChar);
            var raw = Encoding.UTF8.GetString(RawContent).Trim(bomChar);

            if (string.Equals(trim, raw, StringComparison.Ordinal))
            {
                return null;
            }

            return new Change
            {
                FilePath = FilePath,
                FileContents = fileContents,
            };
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