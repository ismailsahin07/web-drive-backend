using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;

namespace Drive.Modules.Files.Application.Items
{
    internal sealed class AvailableNameFinder(IDriveItemRepository items)
    {
        public const int MaxAttempts = 100;

        public async Task<ItemName?> FindAsync(DriveId driveId, ItemId folderId, ItemName desiredName, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(desiredName);

            if (!await items.NameExistsAsync(driveId, folderId, desiredName, cancellationToken))
            {
                return desiredName;
            }

            for (var number = 1; number <= MaxAttempts; number++)
            {
                var candidate = desiredName.WithNumberSuffix(number);
                if (!await items.NameExistsAsync(driveId, folderId, candidate, cancellationToken))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
