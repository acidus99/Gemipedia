namespace Gemipedia.Models;

public class VideoItem : MediaItem, IArticleLinks
{
    public string VideoUrl { get; set; }
    public string VideoDescription { get; set; }
}