using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Folders.CreateFolder
{
    internal sealed class CreateFolderHandler(
        DriveAccess driveAccess,
        IDriveItemRepository items,
        ItemTree itemTree,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : ICommandHandler<CreateFolderCommand, ItemId>
    {
        public async Task<Result<ItemId>> HandleAsync(CreateFolderCommand command, CancellationToken cancellationToken)
        {
            var nameResult = ItemName.Create(command.Name);
            if (nameResult.IsFailure)
            {
                return nameResult.Error;
            }

            var driveResult = await driveAccess.GetOwnedDriveAsync(command.DriveId, cancellationToken);
            if (driveResult.IsFailure)
            {
                return driveResult.Error;
            }

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

            var folderResult = DriveItem.CreateFolder(
                ItemId.New(),
                parent,
                nameResult.Value,
                currentUser.Id,
                timeProvider.GetUtcNow());

            if (folderResult.IsFailure)
            {
                return folderResult.Error;
            }

            var folder = folderResult.Value;
            items.Add(folder);

            var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                return saveResult.Error;
            }

            return folder.Id;
        }
    }
}
