using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Domain.Drives
{
    internal static class StorageDriveErrors
    {
        public static readonly Error QuotaExceeded = Error.Forbidden(
            "Files.StorageDrive.QuotaExceeded",
            "There is not enough free storage space for this file.");
    }
}
