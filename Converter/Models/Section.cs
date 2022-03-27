using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class Section
    {
        public List<Section> SubSections { get; set; }=  new List<Section>();

        public List<SectionItem> Items { get; set; } = new List<SectionItem>();

        //special sections don't have titles. the intro section is a special section
        public bool IsSpecial { get; set; } = false;

        public int SectionDepth { get; set; }
        public string Title { get; set; }
    }
}
