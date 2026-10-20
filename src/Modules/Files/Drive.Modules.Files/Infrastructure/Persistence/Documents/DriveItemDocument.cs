using System.Text.Json.Serialization;

namespace Drive.Modules.Files.Infrastructure.Persistence.Documents
{
    internal sealed class DriveItemDocument
    {
        public required string Id { get; init; }

        public string Type { get; init; } = DocumentTypes.Item;

        public required Guid DriveId { get; init; }

        public Guid? ParentId { get; init; }

        public required string Kind { get; init; }

        public required string Name { get; init; }

        public string? NameKey { get; init; }

        public required string SortKey { get; init; }

        public required string Status { get; init; }

        public long SizeBytes { get; init; }

        public string? ContentType { get; init; }

        public string? BlobName { get; init; }

        public required Guid CreatedBy { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }

        public required Guid ModifiedBy { get; init; }

        public required DateTimeOffset ModifiedAt { get; init; }

        public DateTimeOffset? TrashedAt { get; init; }

        [JsonPropertyName("_etag")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ETag { get; init; }
    }
}
