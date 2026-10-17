using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Uploads.CompleteUpload
{
    public sealed record CompleteUploadCommand(DriveId DriveId, ItemId ItemId) : ICommand;
}
