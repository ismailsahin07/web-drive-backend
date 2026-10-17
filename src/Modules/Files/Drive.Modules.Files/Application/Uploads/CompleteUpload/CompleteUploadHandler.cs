using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Uploads.CompleteUpload
{
    internal sealed class CompleteUploadHandler(
        DriveAccess driveAccess,
        IStorageDriveRepository drives,
        IDriveItemRepository items,
        IUnitOfWork unitOfWork,
        IBlobStorage blobStorage,
        TimeProvider timeProvider)
        : ICommandHandler<CompleteUploadCommand>
    {
        public async Task<Result> HandleAsync(CompleteUploadCommand command, CancellationToken cancellationToken)
        {
            var driveResult = await driveAccess.GetOwnedDriveAsync(command.DriveId, cancellationToken);
            if (driveResult.IsFailure)
            {
                return driveResult.Error;
            }

            var drive = driveResult.Value;

            var item = await items.GetAsync(command.DriveId, command.ItemId, cancellationToken);
            if (item is null)
            {
                return FilesErrors.ItemNotFound;
            }

            if (item.Kind != ItemKind.File)
            {
                return FilesErrors.NotAFile;
            }

            if (item.Status != ItemStatus.PendingUpload)
            {
                return Result.Success();
            }

            var blobName = item.BlobName
                ?? throw new InvalidOperationException($"File {item.Id} has no blob name.");

            var actualSizeBytes = await blobStorage.GetSizeAsync(blobName, cancellationToken);
            if (actualSizeBytes is not { } sizeBytes)
            {
                return UploadErrors.NotUploaded;
            }

            var markResult = item.MarkUploaded(sizeBytes, timeProvider.GetUtcNow());
            if (markResult.IsFailure)
            {
                return markResult.Error;
            }

            drive.CommitReservation(item.SizeBytes);

            items.Update(item);
            drives.Update(drive);

            return await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
