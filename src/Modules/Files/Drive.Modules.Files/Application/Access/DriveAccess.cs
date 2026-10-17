using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Access
{
    internal sealed class DriveAccess(IStorageDriveRepository drives, ICurrentUser currentUser)
    {
        public async Task<Result<StorageDrive>> GetOwnedDriveAsync(DriveId driveId, CancellationToken cancellationToken)
        {
            var drive = await drives.GetAsync(driveId, cancellationToken);

            if (drive is null || drive.OwnerId != currentUser.Id)
            {
                return FilesErrors.DriveNotFound;
            }

            return drive;
        }
    }
}
