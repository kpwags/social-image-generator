using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SocialImageGenerator.Models;
using System;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SocialImageGenerator;

internal class Program
{
    private static PostData? _data;
    private static ReadingLogData? _readingLogData;
    private static string _destinationDirectory = "";
    private static DirectorySettings? _directorySettings;

    static async Task Main()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        
        _directorySettings = config.GetRequiredSection("Directories").Get<DirectorySettings>();

        if (_directorySettings is null)
        {
            Console.WriteLine("Unable to read settings");
            return;
        }
        
        Console.Write("Please enter '1' for blog post, '2' for reading log, or '3' for note (1): ");

        var post = Console.ReadLine();
        var postType = PostType.BlogPost;

        if (!string.IsNullOrWhiteSpace(post))
        {
            postType = (PostType)int.Parse(post);
        }

        _destinationDirectory = GetDestinationDirectory(postType);

        if (string.IsNullOrWhiteSpace(_destinationDirectory))
        {
            Console.WriteLine("Invalid destination directory");
            return;
        }

        switch (postType)
        {
            case PostType.BlogPost:
            case PostType.Note:
                _data = GetPostData();
                break;
            
            case PostType.ReadingLog:
                _readingLogData = GetReadingLogData();
                break;
            
            default:
                throw new Exception("Invalid Post Type");
        }
        
        await BuildImage(postType);
    }

    static string GetDestinationDirectory(PostType postType)
    {
        if (_directorySettings is null)
        {
            throw new Exception("Unable to read settings");
        }

        var folder = _directorySettings.Posts;
        
        if (postType == PostType.ReadingLog)
        {
            folder = _directorySettings.ReadingLogs;
        }

        if (postType == PostType.Note)
        {
            folder = _directorySettings.Notes;
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"{_directorySettings.Mac}/{folder}";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{_directorySettings.Windows}/{folder}";
        }
        
        throw new Exception("Invalid Operating System");
    }

    static DateTime GetPostDate(string enteredDate)
    {
        if (!DateTime.TryParse(enteredDate, out DateTime postDate))
        {
            postDate = DateTime.Now;
        }

        return postDate;
    }

    static PostData GetPostData()
    {
        Console.Write($"Enter the post's date ({DateTime.Now:yyyy-MM-dd}): ");

        var date = Console.ReadLine();

        var postDate = GetPostDate(date ?? "");

        Console.Write($"Enter the post's Title: ");

        var postTitle = Console.ReadLine();

        var slug = BuildUrlSlug(postTitle ?? "");

        Console.Write($"Enter the post's slug ({slug}): ");

        var postSlug = Console.ReadLine();

        return new PostData
        {
            Title = postTitle ?? "",
            Slug = postSlug == "" ? slug : postSlug ?? "",
            PostDate = postDate,
        };
    }

    static ReadingLogData GetReadingLogData()
    {
        Console.Write($"Enter the reading log date ({DateTime.Now:MMMM d, yyyy}): ");
        
        var postDate = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(postDate))
        {
            postDate = DateTime.Now.ToString("MMMM d, yyyy");
        }
        
        Console.Write($"Enter the reading log number: ");
        
        var number = Console.ReadLine();

        var postTitle = DateTime.Parse(postDate).ToString("MMMM d, yyyy");
        
        return new ReadingLogData
        {
            Title = $"{postTitle} (#{number})",
            ReadingLogNumber = int.Parse(number ?? "0"),
        };
    }

    static async Task BuildImage(PostType postType)
    {
        if ((postType == PostType.BlogPost || postType == PostType.Note) && _data is null)
        {
            throw new Exception("Post Data is null");
        }
        
        if (postType == PostType.ReadingLog && _readingLogData is null)
        {
            throw new Exception("Reading Log Data is null");
        }

        using var image = Image.Load("Template.jpg");

        var fontCollection = new FontCollection();

        fontCollection.Add("SourceCodePro.ttf");

        if (fontCollection.TryGet("Source Code Pro", out FontFamily family))
        {
            var postTitle = _data?.Title ?? _readingLogData?.Title ?? "";
            
            var font = family.CreateFont(60, FontStyle.Bold);
            var leadingTitleFont = family.CreateFont(36, FontStyle.Bold);
            var dateFont = family.CreateFont(32, FontStyle.Regular);

            var leadingTitleOptions = new RichTextOptions(leadingTitleFont)
            {
                Origin = new PointF(40, 60),
                WrappingLength = 1120,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            bool hasLeadingTitle = postType == PostType.ReadingLog || postTitle.StartsWith("What I Learned:");

            FontRectangle? rect = null;
            
            PatternBrush brush = Brushes.Horizontal(Color.White, Color.White);
            
            if (hasLeadingTitle)
            {
                var leadingText = "Reading Log:";
                if (_data is not null && _data.Title.StartsWith("What I Learned"))
                {
                    postTitle = postTitle.Replace("What I Learned: ", "");
                    leadingText = "What I Learned:";
                }
                
                rect = TextMeasurer.MeasureSize(leadingText, leadingTitleOptions);

                image.Mutate(x => x.DrawText(new DrawingOptions(), leadingTitleOptions, leadingText, brush, null));
            }

            var leadingTextHeight = rect?.Height ?? 0;

            var titleOptions = new RichTextOptions(font)
            {
                Origin = new PointF(40, 80 + leadingTextHeight),
                WrappingLength = 1120,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            rect = TextMeasurer.MeasureSize(postTitle, titleOptions);
            
            image.Mutate(x => x.DrawText(new DrawingOptions(), titleOptions, postTitle, brush, null));

            if (postType != PostType.ReadingLog)
            {
                var dateOptions = new RichTextOptions(dateFont)
                {
                    Origin = new PointF(40, 150 + rect.Value.Height),
                    WrappingLength = 1120,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
            
                image.Mutate(x => x.DrawText(new DrawingOptions(), dateOptions, (_data?.PostDate ?? DateTime.Now).ToString("MMMM d, yyyy"), brush, null));
            }
        }

        using var ms = new MemoryStream();

        await image.SaveAsync(ms, new JpegEncoder());

        await image.SaveAsJpegAsync(BuildFilePath(postType));
    }

    static string BuildFilePath(PostType postType)
    {
        if ((postType == PostType.BlogPost || postType == PostType.Note) && _data is null)
        {
            throw new Exception("Post Data is null");
        }
        
        if (postType == PostType.ReadingLog && _readingLogData is null)
        {
            throw new Exception("Reading Log Data is null");
        }

        if (postType == PostType.BlogPost || postType == PostType.Note)
        {
            if (!Directory.Exists(_destinationDirectory))
            {
                Directory.CreateDirectory(_destinationDirectory);
            }
            
            return Path.Join(_destinationDirectory, _data?.Filename);
        }
        
        return Path.Join(_destinationDirectory, $"{_readingLogData?.ReadingLogNumber}.jpg");
    }

    static string BuildUrlSlug(string title)
    {
        var noSpecialChars = new string(title.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-').ToArray());

        return noSpecialChars.Replace(" ", "-").Replace("---", "-").ToLower();
    }
}
