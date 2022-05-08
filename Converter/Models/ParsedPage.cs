using System;
using System.Linq;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public class ParsedPage
    {
        private int currSection = 0;

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

        public int GetReferenceCount()
        {
            int count = 0;
            foreach(var section in Sections)
            {
                count += GetSectionCount(section);
            }
            return count;
        }

        private int GetSectionCount(Section section)
        {
            int subSectionCount = 0;
            foreach(var sub in section.SubSections)
            {
                subSectionCount += GetSectionCount(sub);
            }
            return subSectionCount + section.ArticleLinks.Count;
        }

        public Section GetSection(int sectionNum)
        {
            currSection = 0;
            foreach (var sub in Sections)
            {
                var section = GetSectionHelper(sub, sectionNum);
                if(section != null)
                {
                    return section;
                }
            }
            return null;
        }

        private Section GetSectionHelper(Section curr, int lookingFor)
        {
            currSection++;
            if(currSection == lookingFor)
            {
                return curr;
            }
            if(curr.HasSubSections)
            {
                foreach (var sub in curr.SubSections)
                {
                    var section = GetSectionHelper(sub, lookingFor);
                    if (section != null)
                    {
                        return section;
                    }
                }
            }
            return null;
        }


        private void CollectorHelper(Section section, List<MediaItem> images)
        {
            images.AddRange(section.GeneralContent.Where(x => x is MediaItem).Select(x => (MediaItem)x));
            section.Infoboxes.ForEach(x => images.AddRange(x.MediaItems));
            foreach (var subSection in section.SubSections)
            {
                CollectorHelper(subSection, images);
            }
        }
    }
}
