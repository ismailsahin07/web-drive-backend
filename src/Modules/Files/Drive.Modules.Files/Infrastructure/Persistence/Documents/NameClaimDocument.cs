namespace Drive.Modules.Files.Infrastructure.Persistence.Documents
{
    internal sealed class NameClaimDocument
    {
        public required string Id { get; init; }

        public string Type { get; init; } = DocumentTypes.NameClaim;

        public required Guid DriveId { get; init; }

        public required Guid ItemId { get; init; }

        public required string NameKey { get; init; }
    }
}
