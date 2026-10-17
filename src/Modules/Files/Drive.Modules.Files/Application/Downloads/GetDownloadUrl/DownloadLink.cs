namespace Drive.Modules.Files.Application.Downloads.GetDownloadUrl
{
    public sealed record DownloadLink(Uri Url, DateTimeOffset ExpiresAt);
}
