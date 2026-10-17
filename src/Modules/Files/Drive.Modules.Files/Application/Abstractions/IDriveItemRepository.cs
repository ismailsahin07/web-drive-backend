using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Paging;

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

        /// <summary>
        /// Lists the available (not trashed, not uploading) direct children of a folder:
        /// folders first, then by name (case-insensitive), then by ID.
        /// </summary>
        Task<PagedResult<DriveItem>> ListChildrenAsync(
            DriveId driveId,
            ItemId folderId,
            int pageSize,
            string? continuationToken,
            CancellationToken cancellationToken);

        /// <summary>Lists the drive's trashed items, most recently trashed first.</summary>
        Task<PagedResult<DriveItem>> ListTrashAsync(
            DriveId driveId,
            int pageSize,
            string? continuationToken,
            CancellationToken cancellationToken);

        void Add(DriveItem item);

        void Update(DriveItem item);
    }
}
