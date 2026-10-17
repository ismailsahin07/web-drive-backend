using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Application.Storage;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Uploads.InitiateUpload
{
    internal sealed class InitiateUploadHandler(
        DriveAccess driveAccess,
        IStorageDriveRepository drives,
        IDriveItemRepository items,
        ItemTree itemTree,
        IUnitOfWork unitOfWork,
        IBlobStorage blobStorage,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        FilesOptions options)
        : ICommandHandler<InitiateUploadCommand, UploadTicket>
    {
        public async Task<Result<UploadTicket>> HandleAsync(InitiateUploadCommand command, CancellationToken cancellationToken)
        {
            var nameResult = ItemName.Create(command.FileName);
            if (nameResult.IsFailure)
            {
                return nameResult.Error;
            }

            if (command.SizeBytes < 0)
            {
                return UploadErrors.InvalidSize;
            }

            if (command.SizeBytes > options.MaxFileSizeBytes)
            {
                return UploadErrors.FileTooLarge;
            }

            var driveResult = await driveAccess.GetOwnedDriveAsync(command.DriveId, cancellationToken);
            if (driveResult.IsFailure)
            {
                return driveResult.Error;
            }

            var drive = driveResult.Value;

            var parent = await items.GetAsync(command.DriveId, command.ParentId, cancellationToken);
            if (parent is null || parent.Status == ItemStatus.PendingUpload)
            {
                return FilesErrors.ItemNotFound;
            }

            if (await itemTree.HasTrashedAncestorAsync(parent, cancellationToken))
            {
                return DriveItemErrors.DestinationNotAvailable;
            }

            if (await items.NameExistsAsync(command.DriveId, parent.Id, nameResult.Value, cancellationToken))
            {
                return FilesErrors.NameTaken;
            }

            var now = timeProvider.GetUtcNow();
            var itemId = ItemId.New();
            var blobName = BlobNames.ForNewFile(drive.Id, itemId);
            var contentType = string.IsNullOrWhiteSpace(command.ContentType)
                ? ContentTypes.Default
                : command.ContentType.Trim();

            var fileResult = DriveItem.CreatePendingFile(
                itemId,
                parent,
                nameResult.Value,
                command.SizeBytes,
                contentType,
                blobName,
                currentUser.Id,
                now);

            if (fileResult.IsFailure)
            {
                return fileResult.Error;
            }

            var reserveResult = drive.ReserveSpace(command.SizeBytes);
            if (reserveResult.IsFailure)
            {
                return reserveResult.Error;
            }

            items.Add(fileResult.Value);
            drives.Update(drive);

            var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                return saveResult.Error;
            }

            var expiresAt = now + options.UploadUrlLifetime;
            var uploadUrl = await blobStorage.CreateUploadUrlAsync(blobName, expiresAt, cancellationToken);

            return new UploadTicket(itemId.Value, uploadUrl, expiresAt);
        }
    }
}
