using System;
using System.Text;

using Gemipedia.NGConverter.Models;

namespace Gemipedia.NGConverter
{
    public class Buffer
    {

        public ArticleLinkCollection Links { get; private set; }

        public string Content => sb.ToString();

        public bool HasContent => (sb.Length > 0);

        public bool HasNewline { get; private set; }

        public bool AllowNewlines { get; set; }

        private StringBuilder sb;

        public Buffer()
        {
            sb = new StringBuilder();
            Links = new ArticleLinkCollection();
            HasNewline = false;
        }

        public void Reset()
        {
            sb.Clear();
            Links = new ArticleLinkCollection();
            HasNewline = false;
        }

        public void Append(string s)
        {
            HasNewline = s.Contains("\n");
            if(!AllowNewlines)
            {
                s = s.Replace('\n',' ');
            }
            sb.Append(s);
        }

        public void AppendLine(string s = "")
        {
            HasNewline = true;
            if (!AllowNewlines)
            {
                s = s.Replace('\n', ' ');
            }
            sb.AppendLine(s);
        }

        public void EnsureEndsWithNewline()
        {
            if(!Content.EndsWith("\n"))
            {
                sb.AppendLine();
            }
        }

    }
}
