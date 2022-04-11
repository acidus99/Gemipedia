﻿using System;
namespace Gemipedia.Converter.Models
{
    public class ArticleLink
    {
        public int Occurences { get; internal set; }

        public string Title { get; private set; }

        internal ArticleLink(string title)
        {
            Title = title;
            Occurences = 1;
        }
    }
}
