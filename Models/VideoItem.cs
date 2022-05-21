using System;
namespace Gemipedia.Models
{
    public class VideoItem : MediaItem, IArticleLinks
    {
        public string VideoUrl { get; set; }
        public string VideoDescription { get; set; }

        public override string Render()
            => $"=> {Url} Video Still: {Caption}\n=> {VideoUrl} Source Video: {VideoDescription}\n";
    }

}

