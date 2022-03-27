using System;
using System.Linq;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class ParsedPage
    {
        public String Title { get; set; }

        public List<Section> Sections { get; set; } = new List<Section>();

        public List<MediaItem> GetAllImages()
        {
            var ret = new List<MediaItem>();
            foreach(var section in Sections)
            {
                CollectorHelper(section, ret);
            }
            return ret;
        }

        private void CollectorHelper(Section section, List<MediaItem> images)
        {
            images.AddRange(section.GetItems().Where(x => x is MediaItem).Select(x => (MediaItem)x));
            foreach(var subSection in section.SubSections)
            {
                CollectorHelper(subSection, images);
            }
        }
    }
}
