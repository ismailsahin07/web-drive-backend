using System.Text.Json.Serialization;

namespace Drive.Modules.Files.Infrastructure.Persistence.Documents
{
    internal sealed class StorageDriveDocument
    {
        public const string FixedId = "drive";

        public string Id { get; init; } = FixedId;

        public string Type { get; init; } = DocumentTypes.Drive;

        public required Guid DriveId { get; init; }

        public required Guid OwnerId { get; init; }

        public required Guid RootFolderId { get; init; }

        public required long QuotaBytes { get; init; }

        public required long UsedBytes { get; init; }

        public required long ReservedBytes { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("_etag")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ETag { get; init; }
    }
}
