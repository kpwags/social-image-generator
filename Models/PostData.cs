using System;

namespace SocialImageGenerator.Models;

public class PostData
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime PostDate { get; set; } = DateTime.Now;

    public string Url => $"kpwags.com/posts/{PostDate.ToString("yyyy")}/{PostDate.ToString("MM")}/{PostDate.ToString("dd")}/{Slug}";

    public string Directory => $"{PostDate.ToString("yyyy")}-{PostDate.ToString("MM")}-{PostDate.ToString("dd")}-{Slug}";
}