namespace Drive.Modules.Files.Application.Abstractions
{
    internal interface IBlobStorage
    {
        /// <summary>Creates a short-lived URL the browser can use to upload (create or write) this one blob, and nothing else.</summary>
        Task<Uri> CreateUploadUrlAsync(string blobName, DateTimeOffset expiresAt, CancellationToken cancellationToken);

        /// <summary>Creates a short-lived, read-only URL that downloads the blob under the given file name and content type.</summary>
        Task<Uri> CreateDownloadUrlAsync(
            string blobName,
            string fileName,
            string contentType,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken);

        /// <summary>Returns the size of the blob in bytes, or null if it does not exist (yet).</summary>
        Task<long?> GetSizeAsync(string blobName, CancellationToken cancellationToken);
    }
}
