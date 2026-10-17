using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Uploads
{
    internal static class UploadErrors
    {
        public static readonly Error InvalidSize = Error.Validation(
            "Files.Upload.InvalidSize",
            "The file size cannot be negative.");

        public static readonly Error FileTooLarge = Error.Validation(
            "Files.Upload.FileTooLarge",
            "The file is larger than the maximum allowed size.");

        public static readonly Error NotUploaded = Error.Conflict(
            "Files.Upload.NotUploaded",
            "The file content has not been uploaded yet.");
    }
}
