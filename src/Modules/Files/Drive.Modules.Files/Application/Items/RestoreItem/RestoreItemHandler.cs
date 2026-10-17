using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.RestoreItem
{
    internal sealed class RestoreItemHandler(
        DriveAccess driveAccess,
        IDriveItemRepository items,
        ItemTree itemTree,
        AvailableNameFinder nameFinder,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : ICommandHandler<RestoreItemCommand>
    {
        public async Task<Result> HandleAsync(RestoreItemCommand command, CancellationToken cancellationToken)
        {
            var driveResult = await driveAccess.GetOwnedDriveAsync(command.DriveId, cancellationToken);
            if (driveResult.IsFailure)
            {
                return driveResult.Error;
            }

            var item = await items.GetAsync(command.DriveId, command.ItemId, cancellationToken);
            if (item is null || item.Status == ItemStatus.PendingUpload)
            {
                return FilesErrors.ItemNotFound;
            }

            if (item.Status != ItemStatus.Trashed)
            {
                return Result.Success();
            }

            var targetFolder = await GetRestoreTargetAsync(driveResult.Value, item, cancellationToken);

            var name = await nameFinder.FindAsync(item.DriveId, targetFolder.Id, item.Name, cancellationToken);
            if (name is null)
            {
                return FilesErrors.NameTaken;
            }

            var userId = currentUser.Id;
            var now = timeProvider.GetUtcNow();

            var restoreResult = item.Restore(userId, now);
            if (restoreResult.IsFailure)
            {
                return restoreResult.Error;
            }

            if (name != item.Name)
            {
                var renameResult = item.Rename(name, userId, now);
                if (renameResult.IsFailure)
                {
                    return renameResult.Error;
                }
            }

            if (item.ParentId != targetFolder.Id)
            {
                var targetAncestors = await itemTree.GetAncestorsAsync(targetFolder, cancellationToken);
                var targetAncestorIds = targetAncestors.Select(ancestor => ancestor.Id).ToList();

                var moveResult = item.MoveTo(targetFolder, targetAncestorIds, userId, now);
                if (moveResult.IsFailure)
                {
                    return moveResult.Error;
                }
            }

            items.Update(item);
            return await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<DriveItem> GetRestoreTargetAsync(StorageDrive drive, DriveItem item, CancellationToken cancellationToken)
        {
            if (item.ParentId is { } parentId)
            {
                var parent = await items.GetAsync(item.DriveId, parentId, cancellationToken);
                if (parent is not null
                    && parent.Status == ItemStatus.Available
                    && !await itemTree.HasTrashedAncestorAsync(parent, cancellationToken))
                {
                    return parent;
                }
            }

            return await items.GetAsync(item.DriveId, drive.RootFolderId, cancellationToken)
                ?? throw new InvalidOperationException($"The root folder of drive {drive.Id} does not exist.");
        }
    }
}
