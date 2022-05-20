using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using AngleSharp.Dom;


namespace Gemipedia.Models
{
    [DebuggerDisplay("Section '{Title}'")]
    public class Section : IArticleLinks
    {

        public List<INode> Nodes = new List<INode>();

        public ArticleLinkCollection Links { get; private set; } = new ArticleLinkCollection();

        public bool HasSubSections => (SubSections.Count > 0);

        //infoboxes 
        public List<InfoboxItem> Infoboxes = new List<InfoboxItem>();

        //content and images
        public List<SectionItem> GeneralContent = new List<SectionItem>();

        public bool HasNavSuggestions
            => NavSuggestions.Count > 0;

        public List<NavSuggestionsItem> NavSuggestions = new List<NavSuggestionsItem>();

        public List<Section> SubSections { get; set; }=  new List<Section>();

        //force processing
        public void AddItems(IEnumerable<SectionItem> items)
            => items.ToList().ForEach(x => AddItem(x));

        private void AddItem(SectionItem item)
        {
            if (item is IArticleLinks && !(item is NavSuggestionsItem))
            {
                Links.Add(((IArticleLinks)item).Links);
            }

            if(item is InfoboxItem)
            {
                Infoboxes.Add((InfoboxItem)item);
            } else if(item is NavSuggestionsItem)
            {
                NavSuggestions.Add((NavSuggestionsItem)item);
            } else
            {
                GeneralContent.Add(item);
            }
        }

        //special sections don't have titles. the intro section is a special section
        public bool IsSpecial { get; set; } = false;

        public int SectionDepth { get; set; }
        public string Title { get; set; }

    }
}
