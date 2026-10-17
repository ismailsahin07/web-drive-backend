using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;

namespace Drive.Modules.Files.Application.Abstractions
{
    internal interface IDriveItemRepository
    {
        Task<DriveItem?> GetAsync(DriveId driveId, ItemId itemId, CancellationToken cancellationToken);

        /// <summary>
        /// True if a non-trashed item (available or still uploading) in the folder already uses
        /// this name, compared case-insensitively via <see cref="ItemName.ComparisonKey"/>.
        /// </summary>
        Task<bool> NameExistsAsync(DriveId driveId, ItemId parentId, ItemName name, CancellationToken cancellationToken);

        void Add(DriveItem item);

        void Update(DriveItem item);
    }
}
