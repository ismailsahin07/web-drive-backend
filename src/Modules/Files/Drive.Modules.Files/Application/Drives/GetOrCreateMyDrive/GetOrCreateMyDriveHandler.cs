using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Drives.GetOrCreateMyDrive
{
    internal sealed class GetOrCreateMyDriveHandler(
        IStorageDriveRepository drives,
        IDriveItemRepository items,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        FilesOptions options)
        : ICommandHandler<GetOrCreateMyDriveCommand, StorageDriveDto>
    {
        public async Task<Result<StorageDriveDto>> HandleAsync(GetOrCreateMyDriveCommand command, CancellationToken cancellationToken)
        {
            var ownerId = currentUser.Id;
            var driveId = PersonalDrives.IdFor(ownerId);

            var existing = await drives.GetAsync(driveId, cancellationToken);
            if (existing is not null)
            {
                return existing.ToDto();
            }

            var now = timeProvider.GetUtcNow();
            var rootFolderId = ItemId.New();

            var drive = StorageDrive.Create(driveId, ownerId, rootFolderId, options.DefaultQuotaBytes, now);
            var rootFolder = DriveItem.CreateRootFolder(rootFolderId, driveId, ownerId, now);

            drives.Add(drive);
            items.Add(rootFolder);

            var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (saveResult.IsSuccess)
            {
                return drive.ToDto();
            }

            if (saveResult.Error == FilesErrors.ConcurrencyConflict)
            {
                var createdMeanwhile = await drives.GetAsync(driveId, cancellationToken);
                if (createdMeanwhile is not null)
                {
                    return createdMeanwhile.ToDto();
                }
            }

            return saveResult.Error;
        }
    }
}
