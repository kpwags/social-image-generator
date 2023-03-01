namespace SocialImageGenerator.Models;

public class ReadingLogData
{
    public string Title { get; set; } = string.Empty;
    
    public int ReadingLogNumber { get; set; }
    
    public string Url => $"kpwags.com/reading-log/{ReadingLogNumber}";
}