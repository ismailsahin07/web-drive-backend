namespace Drive.Modules.Files.Application.Drives
{
    public sealed record StorageDriveDto(
        Guid Id,
        Guid OwnerId,
        Guid RootFolderId,
        long QuotaBytes,
        long UsedBytes,
        long ReservedBytes,
        long AvailableBytes,
        DateTimeOffset CreatedAt);
}
