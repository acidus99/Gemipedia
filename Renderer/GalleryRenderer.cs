using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Gemipedia.Models;

namespace Gemipedia.Renderer
{
    public class GalleryRenderer
    {
        TextWriter Writer;
        ParsedPage Page;

        public void RenderGallery(ParsedPage parsedPage, TextWriter writer)
        {
            Writer = writer;
            Page = parsedPage;
            Writer.WriteLine($"# Image Gallery: {Page.Title}");
            Writer.WriteLine($"=> {CommonUtils.ArticleUrl(Page.Title)} Back to article");
            Writer.WriteLine();
            foreach(var media in Page.GetAllImages())
            {
                if (media is VideoItem)
                {
                    var video = (VideoItem)media;
                    Writer.WriteLine($"=> {video.Url} Video Still: {video.Caption}");
                    Writer.WriteLine($"=> {video.VideoUrl} Source Video: {video.VideoDescription}"); ;
                }
                else
                {
                    Writer.WriteLine($"=> {media.Url} {media.Caption}");
                }
            }
        }
    }
}
