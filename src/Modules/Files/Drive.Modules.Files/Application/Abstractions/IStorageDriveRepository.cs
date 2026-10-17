using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;

namespace Drive.Modules.Files.Application.Abstractions
{
    internal interface IStorageDriveRepository
    {
        Task<StorageDrive?> GetAsync(DriveId id, CancellationToken cancellationToken);

        void Add(StorageDrive drive);

        void Update(StorageDrive drive);
    }
}
