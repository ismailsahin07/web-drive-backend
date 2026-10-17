using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.TrashItem
{
    internal sealed class TrashItemHandler(
        DriveAccess driveAccess,
        IDriveItemRepository items,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : ICommandHandler<TrashItemCommand>
    {
        public async Task<Result> HandleAsync(TrashItemCommand command, CancellationToken cancellationToken)
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

            if (item.Status == ItemStatus.Trashed)
            {
                return Result.Success();
            }

            var trashResult = item.Trash(currentUser.Id, timeProvider.GetUtcNow());
            if (trashResult.IsFailure)
            {
                return trashResult.Error;
            }

            items.Update(item);
            return await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
