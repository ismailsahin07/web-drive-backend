using Drive.Modules.Files.Application.Abstractions;

namespace Drive.Modules.Files.UnitTests.TestDoubles
{
    internal sealed class FakeBlobStorage : IBlobStorage
    {
        private readonly Dictionary<string, long> _blobs = [];
        private readonly List<string> _uploadUrlsIssued = [];

        public IReadOnlyList<string> UploadUrlsIssued => _uploadUrlsIssued;

        public string? LastDownloadFileName { get; private set; }

        public string? LastDownloadContentType { get; private set; }

        public void SimulateUpload(string blobName, long sizeBytes) => _blobs[blobName] = sizeBytes;

        public Task<Uri> CreateUploadUrlAsync(string blobName, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            _uploadUrlsIssued.Add(blobName);
            return Task.FromResult(new Uri($"https://blobs.test/files/{blobName}?sp=cw"));
        }

        public Task<Uri> CreateDownloadUrlAsync(
            string blobName,
            string fileName,
            string contentType,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            LastDownloadFileName = fileName;
            LastDownloadContentType = contentType;
            return Task.FromResult(new Uri($"https://blobs.test/files/{blobName}?sp=r"));
        }

        public Task<long?> GetSizeAsync(string blobName, CancellationToken cancellationToken) =>
            Task.FromResult(_blobs.TryGetValue(blobName, out var sizeBytes) ? sizeBytes : (long?)null);
    }
}
