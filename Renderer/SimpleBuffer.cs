using System;
using System.Text;

using Gemipedia.Models;

namespace Gemipedia.Renderer
{
    public class SimpleBuffer
    {
        public string Content => sb.ToString();

        public bool HasContent => (sb.Length > 0);

        public bool AtLineStart
            => !HasContent || Content.EndsWith('\n');

        private StringBuilder sb;

        public SimpleBuffer()
        {
            sb = new StringBuilder();
        }

        public void Reset()
            => sb.Clear();

        public void Append(string s)
            => sb.Append(s);

        public void AppendLine(string s = "")
            => sb.AppendLine(s);

        public void PrependLine(string s = "")
        {
            var existing = sb.ToString();
            sb.Clear();
            sb.AppendLine(s);
            sb.Append(existing);
        }

        public void EnsureAtLineStart()
        {
            if (!AtLineStart)
            {
                sb.AppendLine();
            }
        }
    }
}
