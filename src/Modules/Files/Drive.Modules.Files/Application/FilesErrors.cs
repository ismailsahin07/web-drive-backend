using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application
{
    internal static class FilesErrors
    {
        public static readonly Error DriveNotFound = Error.NotFound(
            "Files.DriveNotFound",
            "The drive was not found.");

        public static readonly Error ItemNotFound = Error.NotFound(
            "Files.ItemNotFound",
            "The item was not found.");

        public static readonly Error NotAFile = Error.Validation(
            "Files.NotAFile",
            "The item is a folder, not a file.");

        public static readonly Error NotAFolder = Error.Validation(
            "Files.NotAFolder",
            "The item is a file, not a folder.");

        public static readonly Error NameTaken = Error.Conflict(
            "Files.NameTaken",
            "An item with this name already exists in this folder.");

        public static readonly Error ConcurrencyConflict = Error.Conflict(
            "Files.ConcurrencyConflict",
            "The item was changed by someone else at the same time. Reload and try again.");
    }
}
