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

        public bool AtLineStart
            => !HasContent || Content.EndsWith('\n');

        public bool InBlockquote { get; set; } = false;

        private StringBuilder sb;

        public Buffer()
        {
            sb = new StringBuilder();
            Links = new ArticleLinkCollection();
        }

        public void Reset()
        {
            sb.Clear();
            Links = new ArticleLinkCollection();
        }

        public void Append(string s)
        {
            HandleBlockQuote(s);
            sb.Append(s);
        }


        public void AppendLine(string s = "")
        {
            HandleBlockQuote(s);
            sb.AppendLine(s);
        }

        public void EnsureAtLineStart()
        {
            if(!AtLineStart)
            {
                sb.AppendLine();
            }
        }

        public void HandleBlockQuote(string s)
        {
            if(InBlockquote && AtLineStart && s.Trim().Length > 0)
            {
                sb.Append(">");
            }
        }

    }
}
