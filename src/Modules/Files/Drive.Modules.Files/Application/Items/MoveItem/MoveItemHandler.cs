using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.MoveItem
{
    internal sealed class MoveItemHandler(
        DriveAccess driveAccess,
        IDriveItemRepository items,
        ItemTree itemTree,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : ICommandHandler<MoveItemCommand>
    {
        public async Task<Result> HandleAsync(MoveItemCommand command, CancellationToken cancellationToken)
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

            if (await itemTree.HasTrashedAncestorAsync(item, cancellationToken))
            {
                return DriveItemErrors.NotAvailable;
            }

            var destination = await items.GetAsync(command.DriveId, command.DestinationFolderId, cancellationToken);
            if (destination is null || destination.Status == ItemStatus.PendingUpload)
            {
                return FilesErrors.ItemNotFound;
            }

            var destinationAncestors = await itemTree.GetAncestorsAsync(destination, cancellationToken);
            if (destinationAncestors.Any(ancestor => ancestor.Status == ItemStatus.Trashed))
            {
                return DriveItemErrors.DestinationNotAvailable;
            }

            var isAlreadyThere = item.ParentId == destination.Id;
            if (!item.IsRoot
                && !isAlreadyThere
                && await items.NameExistsAsync(command.DriveId, destination.Id, item.Name, cancellationToken))
            {
                return FilesErrors.NameTaken;
            }

            var destinationAncestorIds = destinationAncestors.Select(ancestor => ancestor.Id).ToList();
            var moveResult = item.MoveTo(destination, destinationAncestorIds, currentUser.Id, timeProvider.GetUtcNow());
            if (moveResult.IsFailure)
            {
                return moveResult.Error;
            }

            items.Update(item);
            return await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
