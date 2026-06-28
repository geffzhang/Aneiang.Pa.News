namespace Pa.HotNews.Blazor.Models;

/// <summary>
/// 新闻条目模型（匹配 Aneiang.Pa 4.0 Recipe 字段映射）。
/// Aneiang.Pa 4.0 不再内置此类型，由使用方自行定义并通过 FieldMapper 映射。
/// </summary>
public sealed class NewsItem
{
    /// <summary>标题</summary>
    public string? Title { get; set; }

    /// <summary>链接</summary>
    public string? Url { get; set; }
}