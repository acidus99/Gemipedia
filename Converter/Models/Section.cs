using System;
using System.Linq;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class Section :ILinkedArticles
    {
        public List<Section> SubSections { get; set; }=  new List<Section>();

        List<SectionItem> items = new List<SectionItem>();

        LinkedArticles links = new LinkedArticles();

        //force processing
        public void AddItem(SectionItem item)
        {
            if(item == null)
            {
                return;
            }
            if(item is ContentItem)
            {
                links.AddRange(((ContentItem)item).LinkedArticles);
            }
            items.Add(item);
        }

        public List<SectionItem> GetItems()
            => items;

        public int MediaCount
            => items.Where(x => (x is MediaItem)).Count();

        //special sections don't have titles. the intro section is a special section
        public bool IsSpecial { get; set; } = false;

        public int SectionDepth { get; set; }
        public string Title { get; set; }

        public List<string> LinkedArticles
            => links.GetLinks();
    }
}
