using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.RenameItem
{
    internal sealed class RenameItemHandler(
        DriveAccess driveAccess,
        IDriveItemRepository items,
        ItemTree itemTree,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : ICommandHandler<RenameItemCommand>
    {
        public async Task<Result> HandleAsync(RenameItemCommand command, CancellationToken cancellationToken)
        {
            var nameResult = ItemName.Create(command.NewName);
            if (nameResult.IsFailure)
            {
                return nameResult.Error;
            }

            var newName = nameResult.Value;

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

            if (await itemTree.HasTrashedAncestorAsync(item, cancellationToken))
            {
                return DriveItemErrors.NotAvailable;
            }

            var isDifferentName = newName.ComparisonKey != item.Name.ComparisonKey;
            if (item.ParentId is { } parentId
                && isDifferentName
                && await items.NameExistsAsync(command.DriveId, parentId, newName, cancellationToken))
            {
                return FilesErrors.NameTaken;
            }

            var renameResult = item.Rename(newName, currentUser.Id, timeProvider.GetUtcNow());
            if (renameResult.IsFailure)
            {
                return renameResult.Error;
            }

            items.Update(item);
            return await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
