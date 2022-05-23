using System;
using System.Collections.Generic;

namespace Gemipedia.API.Models
{
	public class FeaturedContent
	{
		public ArticleSummary FeaturedArticle { get; set; }

		public List<ArticleSummary> PopularArticles { get; set; }
	}
}

