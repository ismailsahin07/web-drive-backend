namespace Drive.Modules.Files.Application.Items
{
    public sealed record DriveItemDto(
        Guid Id,
        Guid DriveId,
        Guid? ParentId,
        string Kind,
        string Name,
        string Status,
        long SizeBytes,
        string? ContentType,
        DateTimeOffset CreatedAt,
        Guid CreatedBy,
        DateTimeOffset ModifiedAt,
        Guid ModifiedBy,
        DateTimeOffset? TrashedAt);
}
