using System.Collections.Generic;

namespace Gemipedia.API.Models;

/// <summary>
/// Represents the featured content for the day
/// </summary>
public class FeaturedContent
{
    /// <summary>
    /// The featured article of the day
    /// </summary>
    public ArticleSummary FeaturedArticle { get; set; }

    /// <summary>
    /// The most popular articles of the previous day
    /// </summary>
    public List<ArticleSummary> PopularArticles { get; set; }
}