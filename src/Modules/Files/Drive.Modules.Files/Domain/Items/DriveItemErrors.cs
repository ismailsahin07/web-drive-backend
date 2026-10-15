using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Domain.Items
{
    internal static class DriveItemErrors
    {
        public static readonly Error RootCannotBeModified = Error.Forbidden(
            "Files.DriveItem.RootCannotBeModified",
            "The root folder cannot be renamed, moved or trashed.");

        public static readonly Error NotAvailable = Error.Conflict(
            "Files.DriveItem.NotAvailable",
            "The item is not available. It may be in the trash or still uploading.");

        public static readonly Error DestinationNotFolder = Error.Validation(
            "Files.DriveItem.DestinationNotFolder",
            "The destination must be a folder.");

        public static readonly Error DestinationNotAvailable = Error.Conflict(
            "Files.DriveItem.DestinationNotAvailable",
            "The destination folder is in the trash or still being created.");

        public static readonly Error DifferentDrive = Error.Validation(
            "Files.DriveItem.DifferentDrive",
            "Items cannot be moved between different drives.");

        public static readonly Error CannotMoveIntoItself = Error.Conflict(
            "Files.DriveItem.CannotMoveIntoItself",
            "A folder cannot be moved into itself or into one of its subfolders.");

        public static readonly Error UploadSizeMismatch = Error.Validation(
            "Files.DriveItem.UploadSizeMismatch",
            "The uploaded file size does not match the size declared when the upload started.");
    }
}
