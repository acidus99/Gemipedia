using System;
using System.Linq;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class LinkedArticles
    {
        List<string> titles = new List<string>();

        public void AddLink(string title)
        {
            if(String.IsNullOrEmpty(title))
            {
                throw new ApplicationException("adding an empty title link!");
            }
            titles.Add(title);
        }

        public List<string> GetLinks()
            => titles.Distinct().OrderBy(x => x).ToList();

    }
}
