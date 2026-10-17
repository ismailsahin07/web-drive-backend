using Drive.Modules.Files.Domain.Drives;

namespace Drive.Modules.Files.Application.Drives
{
    internal static class StorageDriveMappings
    {
        public static StorageDriveDto ToDto(this StorageDrive drive) => new(
            drive.Id.Value,
            drive.OwnerId.Value,
            drive.RootFolderId.Value,
            drive.QuotaBytes,
            drive.UsedBytes,
            drive.ReservedBytes,
            drive.AvailableBytes,
            drive.CreatedAt);
    }
}
