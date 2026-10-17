using Drive.Modules.Files.Contracts;

namespace Drive.Modules.Files.Application.Storage
{
    internal static class BlobNames
    {
        public static string ForNewFile(DriveId driveId, ItemId itemId) =>
            $"{driveId}/{itemId}/{Guid.CreateVersion7():N}";
    }
}
