using System;
using System.Linq;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class Section : IArticleLinks
    {

        public ArticleLinkCollection ArticleLinks { get; private set; } = new ArticleLinkCollection();

        public bool HasSubSections => (SubSections.Count > 0);

        public List<Section> SubSections { get; set; }=  new List<Section>();

        List<SectionItem> items = new List<SectionItem>();

        //force processing
        public void AddItems(IEnumerable<SectionItem> items)
            => items.ToList().ForEach(x => AddItem(x));

        private void AddItem(SectionItem item)
        {
            if (item is IArticleLinks)
            {
                ArticleLinks.MergeCollection(((IArticleLinks)item).ArticleLinks);
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

    }
}
