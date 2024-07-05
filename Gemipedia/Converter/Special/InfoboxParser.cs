using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Gemipedia.Models;

namespace Gemipedia.Converter.Special;

public class InfoboxParser
{
    InfoboxItem infobox = new InfoboxItem();
    Buffer buffer = new Buffer();
    bool shouldSkipFirst = false;

    TextExtractor textExtractor = new TextExtractor
    {
        ShouldCollapseNewlines = true
    };

    public InfoboxItem Parse(HtmlElement table)
    {
        var tableBodyRows = table.QuerySelector("tbody")?.Children ?? null;

        if (tableBodyRows == null)
        {
            return null;
        }

        infobox.CustomTitle = ExtractTitle(table, tableBodyRows);
        ParseTableRows(tableBodyRows);

        return infobox;
    }

    public static bool IsInfobox(HtmlElement table)
        //fr wikipedia uses infobox_v2
        => table.ClassName?.Contains("infobox") ?? false;

    //=====================================================================

    private void AddHeader(IElement row)
    {
        buffer.Reset();
        textExtractor.Extract(row);
        buffer.EnsureAtLineStart();
        buffer.AppendLine($"### {textExtractor.Content}");
        buffer.Links.Add(textExtractor);
        infobox.AddItem(new ContentItem(buffer));
    }

    private void AddMedia(IElement row)
    {
        //check for a montage
        var multi = row.QuerySelector("div.thumb.tmulti");
        if (multi != null)
        {
            infobox.AddItems(MediaParser.ConvertMontage(multi));
            return;
        }

        var imgContainer = row.Children[0].Children[0];
        var captionContainer = (row.Children[0].ChildElementCount >= 2) ? row.Children[0].Children[1] : null;

        infobox.AddItem(MediaParser.ConvertMedia(imgContainer, captionContainer));
    }

    private void AddTwoCells(IElement left, IElement right)
    {
        if (left.NodeName.ToLower() == "th")
        {
            AddNameValue(left, right);
        }
        else if (IsComparingRow(left, right))
        {
            AddTwoRichCells(left, right);
        }
        else
        {
            AddNameValue(left, right);
        }
    }

    private void AddRichCell(string label, RichContent content)
    {
        if (content.NoContent)
        {
            //just a placeholder label
            infobox.AddItem(new ContentItem
            {
                Content = label + ":\n"
            });
            return;
        }

        //Shoudl the label and content be on the same line or not?
        var labelSuffix = content.IsSingleLine ? ": " : ":\n";
        infobox.AddItem(new ContentItem
        {
            Links = textExtractor.Links,
            Content = label + labelSuffix
        });

        infobox.AddItems(content.Items);
    }

    private void AddTwoRichCells(IElement leftCell, IElement rightCell)
    {
        var content = ParseRichCell(leftCell);
        AddRichCell("[Left Column]", content);

        content = ParseRichCell(rightCell);
        AddRichCell("[Right Column]", content);
    }

    private void AddNameValue(IElement nameCell, IElement valueCell)
    {
        //step 1, extract out the name
        textExtractor.Extract(nameCell);
        var label = CleanLabel(textExtractor.Content);

        var valueContent = ParseRichCell(valueCell);

        if (label.Length > 0)
        {
            if (valueContent.NoContent)
            {
                //just a placeholder label
                infobox.AddItem(new ContentItem
                {
                    Links = textExtractor.Links,
                    Content = label + ":" + "\n"
                });
                return;
            }

            //Should the label and content be on the same line or not?
            var labelSuffix = valueContent.IsSingleLine ? ": " : ":\n";
            infobox.AddItem(new ContentItem
            {
                Links = textExtractor.Links,
                Content = label + labelSuffix
            });
        }

        infobox.AddItems(valueContent.Items);
    }

    private RichContent ParseRichCell(IElement cell)
    {
        var parser = new HtmlParser
        {
            ConvertListItems = false,
        };
        parser.Parse(cell);

        var items = parser.GetItems();
        var contentItems = items.Where(x => x is ContentItem).Select(x => x as ContentItem).ToList();

        if (contentItems.Count == 0)
        {
            // no interesting content
            return new RichContent
            {
                Items = items,
                NoContent = true
            };
        }
        if (parser.HasGeminiFormatting || contentItems.Count > 1)
        {
            return new RichContent
            {
                Items = EnsureNewline(items)
            };
        }

        //lets see if it has multiple lines or not
        var content = contentItems[0].Content.Trim();

        if (!content.Contains('\n'))
        {
            return new RichContent
            {
                IsSingleLine = true,
                Items = EnsureNewline(items)
            };
        }
        else
        {
            //convert it to lines
            buffer.Reset();
            buffer.Links.Add(contentItems[0]);
            //convert to a list
            foreach (var line in content.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                buffer.AppendLine($"* {line}");
            }

            //remove all the content items (only 1) since since we reformatted that into a list
            items.RemoveAll(x => x is ContentItem);
            items.Add(new ContentItem(buffer));

            return new RichContent
            {
                Items = items
            };
        }
    }

    //ensure that, when a sequence of items is rendered, the last item will make a new line
    private List<SectionItem> EnsureNewline(List<SectionItem> items)
    {
        var content = items.Where(x => x is ContentItem).Select(x => x as ContentItem);
        if (content.Count() > 0 && !content.Last().Content.EndsWith("\n"))
        {
            content.Last().Content += "\n";
        }
        return items;
    }

    private void AddWideValue(IElement valueCell)
    {
        //step 1, extract out the name
        var parser = new HtmlParser
        {
            ConvertListItems = false
        };
        parser.Parse(valueCell);
        infobox.AddItems(EnsureNewline(parser.GetItems()));
    }

    private string CleanLabel(string text)
    {
        text = text.Trim();

        //some labels attempt to look like a bulleted list, even though
        //each entry is a different row, and use a "•" character. remove it
        if (text.StartsWith("•") && text.Length > 1)
        {
            text = text.Substring(1);
        }
        //some label, especially those that use a TD for the name cell instead of a TH,
        //will include a ":" already in the text, if so remove it
        if (text.Length >= 2 && text.EndsWith(':'))
        {
            text = text.Substring(0, text.Length - 1);
        }
        if (text.Length > 1)
        {
            //capitalize the first letter
            text = text.Substring(0, 1).ToUpper() + text.Substring(1);
        }
        else if (text.Length == 1)
        {
            text = text.ToUpper();
        }
        return text;
    }

    private string ExtractTitle(HtmlElement table, IHtmlCollection<IElement> rows)
    {
        //first check for a caption
        var caption = table.QuerySelector("caption")?.TextContent.Trim() ?? null;
        if (!String.IsNullOrEmpty(caption))
        {
            return caption;
        }

        if (rows?.Length >= 1 && rows[0].ChildElementCount == 1)
        {
            textExtractor.Extract(rows[0]);

            var title = textExtractor.Content.Trim();
            if (title.Length > 0)
            {
                buffer.Links.Add(textExtractor);
                shouldSkipFirst = true;
                return title;
            }
        }
        return "";
    }

    //are these 2 cells being compared to each other?
    private bool IsComparingRow(IElement left, IElement right)
        => left.HasAttribute("style") && left.GetAttribute("style").Replace(" ", "").Contains("border-right:1px");

    private bool IsHeader(IElement row, int index)
        => (row.ChildElementCount == 1) && row.Children[0].NodeName == "TH";

    private bool IsMedia(IElement row)
        => (row.ChildElementCount == 1) &&
            (row.Children[0].ChildElementCount >= 1) &&
            (row.Children[0].Children?[0].QuerySelector("img") != null);

    private bool IsNestedTable(IElement row)
        => row.QuerySelector("td table tbody") != null;

    private void ParseNestedTable(IElement row)
        => ParseTableRows(row.QuerySelector("td table tbody")?.Children ?? null, true);

    private void ParseRow(IElement row, int index, bool isNestedTable)
    {
        if (!isNestedTable && shouldSkipFirst && index == 0)
        {
            return;
        }
        if (row.NodeName != "TR")
        {
            throw new ApplicationException("Non row in info box");
        }

        if (row.ChildElementCount == 0)
        {
            return;
        }

        if (IsHeader(row, index))
        {
            AddHeader(row);
        }
        else if (IsNestedTable(row))
        {
            ParseNestedTable(row);
        }
        else if (IsMedia(row))
        {
            AddMedia(row);
        }
        else if (row.Children.Length == 1)
        {
            AddWideValue(row.Children[0]);
        }
        else if (row.Children.Length == 2)
        {
            AddTwoCells(row.Children[0], row.Children[1]);
        }
    }

    private void ParseTableRows(IHtmlCollection<IElement> rows, bool isNestedTable = false)
    {
        if (rows == null)
        {
            return;
        }
        for (int i = 0; i < rows.Length; i++)
        {
            ParseRow(rows[i], i, isNestedTable);
        }
    }

    private class RichContent
    {
        public bool NoContent = false;
        public List<SectionItem> Items;
        public bool IsSingleLine = false;
    }

}
