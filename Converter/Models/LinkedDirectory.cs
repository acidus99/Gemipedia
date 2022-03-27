using System;
using System.Linq;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class LinkedArticles
    {
        List<string> titles = new List<string>();

        public void AddRange(List<string> links)
            => links.ForEach(x => AddLink(x));

        public void AddLink(string title)
        {
            if(String.IsNullOrEmpty(title))
            {
                //TODO: remove once parser more reliable
                throw new ApplicationException("adding an empty title link!");
            }
            titles.Add(title);
        }

        public List<string> GetLinks()
            => titles.Distinct().OrderBy(x => x).ToList();
    }
}
