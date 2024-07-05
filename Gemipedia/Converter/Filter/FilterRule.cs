using System;
namespace Gemipedia.Converter.Filter;

/// <summary>
/// represents a rule of DOM nodes we want to filter
/// </summary>
internal class FilterRule
{
    public string ClassName { get; set; } = null;

    public string ID { get; set; } = null;

    public string TagName { get; set; } = null;

    public bool HasClass
        => !string.IsNullOrEmpty(ClassName);

    public bool HasID
        => !string.IsNullOrEmpty(ID);

    public bool HasTag
        => !string.IsNullOrEmpty(TagName);
}