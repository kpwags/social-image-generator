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
        
        Console.Write("Please enter '1' for blog post or '2' for reading log (1): ");

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
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"{_directorySettings.Mac}/{(postType == PostType.ReadingLog ? _directorySettings.ReadingLogs : _directorySettings.Posts)}";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{_directorySettings.Windows}/{(postType == PostType.ReadingLog ? _directorySettings.ReadingLogs : _directorySettings.Posts)}";
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

        return new ReadingLogData
        {
            Title = $"Reading Log -{Environment.NewLine}{postDate} (#{number})",
            ReadingLogNumber = int.Parse(number ?? "0"),
        };
    }

    static async Task BuildImage(PostType postType)
    {
        if (postType == PostType.BlogPost && _data is null)
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
            var font = family.CreateFont(60, FontStyle.Bold);

            var options = new TextOptions(font)
            {
                Origin = new PointF(40, 60),
                WrappingLength = 1120,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            var postTitle = postType == PostType.ReadingLog ? _readingLogData?.Title : _data?.Title;
            
            var rect = TextMeasurer.Measure(postTitle ?? "", options);

            image.Mutate(x => x.DrawText(options, postTitle ?? "", Color.White));

            var urlFont = family.CreateFont(30, FontStyle.Regular);

            var urlOptions = new TextOptions(urlFont)
            {
                Origin = new PointF(40, 80 + rect.Height),
                WrappingLength = 1120,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            var postUrl = postType == PostType.ReadingLog ? _readingLogData?.Url : _data?.Url;
            
            image.Mutate(x => x.DrawText(urlOptions, postUrl ?? "", Color.White));
        }

        using var ms = new MemoryStream();

        await image.SaveAsync(ms, new JpegEncoder());

        await image.SaveAsJpegAsync(BuildFilePath(postType));
    }

    static string BuildFilePath(PostType postType)
    {
        if (postType == PostType.BlogPost && _data is null)
        {
            throw new Exception("Post Data is null");
        }
        
        if (postType == PostType.ReadingLog && _readingLogData is null)
        {
            throw new Exception("Reading Log Data is null");
        }

        if (postType == PostType.BlogPost)
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
