using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SocialImageGenerator.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SocialImageGenerator;

internal class Program
{
    static PostData? data;

    static async Task Main(string[] args)
    {
        data = GetPostData();

        if (data is null)
        {
            Console.WriteLine("Unable to generate post data");
            return;
        }

        await BuildImage();
    }

    static DateTime GetPostDate(string enteredDate)
    {
        DateTime postDate;

        if (!DateTime.TryParse(enteredDate, out postDate))
        {
            postDate = DateTime.Now;
        }

        return postDate;
    }

    static PostData GetPostData()
    {
        Console.Write($"Enter the post's date (defaults to {DateTime.Now.ToString("yyyy-MM-dd")}): ");

        var date = Console.ReadLine();

        var postDate = GetPostDate(date ?? "");

        Console.Write($"Enter the post's Title: ");

        var postTitle = Console.ReadLine();

        Console.Write($"Enter the post's slug: ");

        var postSlug = Console.ReadLine();

        return new PostData
        {
            Title = postTitle ?? "",
            Slug = postSlug ?? "",
            PostDate = postDate,
        };
    }

    static async Task BuildImage()
    {
        if (data is null)
        {
            throw new Exception("Post Data is null");
        }

        using var image = Image.Load("Template.jpg");

        var fontCollection = new FontCollection();

        fontCollection.Add("worksans.ttf");

        if (fontCollection.TryGet("Work Sans", out FontFamily family))
        {
            var font = family.CreateFont(60, FontStyle.Bold);

            var options = new TextOptions(font)
            {
                Origin = new PointF(40, 60),
                WrappingLength = 600,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            var rect = TextMeasurer.Measure(data.Title, options);

            image.Mutate(x => x.DrawText(options, data.Title, Color.White));

            var urlFont = family.CreateFont(24, FontStyle.Regular);

            var urlOptions = new TextOptions(font)
            {
                Origin = new PointF(40, 80 + rect.Height),
                WrappingLength = 600,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            image.Mutate(x => x.DrawText(data.Url, urlFont, Color.White, new PointF(40, 80 + rect.Height)));
        }

        using var ms = new MemoryStream();

        await image.SaveAsync(ms, new JpegEncoder());

        var imageData = ms.ToArray();

        var decoder = new JpegDecoder();

        await image.SaveAsJpegAsync(BuildFilePath());
    }

    static string BuildFilePath()
    {
        if (data is null)
        {
            throw new Exception("Post Data is null");
        }

        string rootDirectory = string.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            rootDirectory = "/Users/keith/Developer/kpwags.com/public/images/posts";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            rootDirectory = @"C:\Users\keith\Developer\kpwags.com\public\images\posts";
        }
        else
        {
            throw new Exception("Invalid Operating System");
        }

        var directory = Path.Join(rootDirectory, data.Directory);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Join(directory, "social-image.jpg");
    }
}
