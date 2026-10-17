namespace Drive.Modules.Files
{
    public sealed class FilesOptions
    {
        public const string SectionName = "Files";

        public long DefaultQuotaBytes { get; init; } = 15L * 1024 * 1024 * 1024;

        public long MaxFileSizeBytes { get; init; } = 5L * 1024 * 1024 * 1024;

        public TimeSpan UploadUrlLifetime { get; init; } = TimeSpan.FromHours(1);

        public TimeSpan DownloadUrlLifetime { get; init; } = TimeSpan.FromMinutes(10);
    }
}
