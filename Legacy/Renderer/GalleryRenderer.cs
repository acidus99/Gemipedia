using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Gemipedia.Legacy.Models;

namespace Gemipedia.Legacy.Renderer
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
            foreach(var mediaItem in Page.GetAllImages())
            {
                Writer.Write(mediaItem.Render());
            }
        }
    }
}
