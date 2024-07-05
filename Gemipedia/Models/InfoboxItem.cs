using System.Collections.Generic;
using System.Linq;

namespace Gemipedia.Models;

public class InfoboxItem : SectionItem, IArticleLinks
{
    public string CustomTitle { get; set; } = "";

    public ArticleLinkCollection Links { get; private set; } = new ArticleLinkCollection();

    public IEnumerable<ContentItem> ContentItems
        => Items.Where(x => x is ContentItem).Select(x => x as ContentItem);

    public IEnumerable<GeoItem> GeoItems
        => Items.Where(x => x is GeoItem).Select(x => x as GeoItem);

    public IEnumerable<MediaItem> MediaItems
        => Items.Where(x => x is MediaItem).Select(x => x as MediaItem);

    public IEnumerable<NavSuggestionsItem> NavSuggestions
        => Items.Where(x => x is NavSuggestionsItem).Select(x => x as NavSuggestionsItem);

    private List<SectionItem> Items = new List<SectionItem>();

    //force processing
    public void AddItems(IEnumerable<SectionItem> items)
        => items.ToList().ForEach(x => AddItem(x));

    public void AddItem(SectionItem item)
    {
        if(item == null)
        {
            return;
        }

        if (item is IArticleLinks && ((IArticleLinks)item).Links != null && !(item is NavSuggestionsItem))
        {
            Links.Add(((IArticleLinks)item).Links);
        }
        Items.Add(item);
    }
}