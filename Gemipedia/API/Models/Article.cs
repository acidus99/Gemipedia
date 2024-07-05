namespace Gemipedia.API.Models;

/// <summary>
/// Represents a Wikipedia Article
/// </summary>
public class Article
{
    /// <summary>
    /// The HTML of the article body
    /// </summary>
    public string HtmlText { get; set; }

    /// <summary>
    /// The page id
    /// </summary>
    public long PageId { get; set; }

    /// <summary>
    /// Title of the article
    /// </summary>
    public string Title { get; set; }
}
