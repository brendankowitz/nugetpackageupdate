using System.Text;
using System.Text.RegularExpressions;

namespace NugetPackageUpdates
{
    public class TextFile : File
    {
        string _content;

        public TextFile(string path, byte[] contents, bool preserveBomChar = false)
            : base(path, contents, preserveBomChar)
        {
            Reset();
        }

        public void Reset()
        {
            _content = Encoding.UTF8.GetString(RawContent);
        }

        public void Replace(string oldText, string newText)
        {
            _content = _content.Replace(oldText, newText);
        }

        public void RegexReplace(Regex regex, string replacementValue)
        {
            _content = regex.Replace(_content, replacementValue);
        }

        protected override string GetContent()
        {
            return _content;
        }
    }
}