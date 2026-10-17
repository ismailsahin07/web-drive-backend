using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Domain.Items;

namespace Drive.Modules.Files.Application.Items
{
    internal sealed class ItemTree(IDriveItemRepository items)
    {
        public const int MaxDepth = 64;

        public async Task<IReadOnlyList<DriveItem>> GetAncestorsAsync(DriveItem item, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);

            var ancestors = new List<DriveItem>();
            var parentId = item.ParentId;

            while (parentId is { } currentParentId)
            {
                if (ancestors.Count >= MaxDepth)
                {
                    throw new InvalidOperationException(
                        $"The folder tree above item {item.Id} is deeper than {MaxDepth} levels or contains a cycle.");
                }

                var parent = await items.GetAsync(item.DriveId, currentParentId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Folder {currentParentId}, an ancestor of item {item.Id}, does not exist.");

                ancestors.Add(parent);
                parentId = parent.ParentId;
            }

            return ancestors;
        }

        public async Task<bool> HasTrashedAncestorAsync(DriveItem item, CancellationToken cancellationToken)
        {
            var ancestors = await GetAncestorsAsync(item, cancellationToken);
            return ancestors.Any(ancestor => ancestor.Status == ItemStatus.Trashed);
        }
    }
}
