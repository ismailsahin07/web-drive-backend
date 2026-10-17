using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Uploads.InitiateUpload
{
    public sealed record InitiateUploadCommand(
        DriveId DriveId,
        ItemId ParentId,
        string FileName,
        long SizeBytes,
        string? ContentType) : ICommand<UploadTicket>;
}
