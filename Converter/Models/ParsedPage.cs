using System;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class ParsedPage
    {
        public String Title { get; set; }

        public List<Section> Sections { get; set; } = new List<Section>();
    }
}
