﻿namespace Gemipedia.Models;

public class MediaItem : SectionItem, IArticleLinks
{
    public string Url { get; set; }
    public string Caption { get; set; }
    public ArticleLinkCollection Links { get; set; } = new ArticleLinkCollection();
}

