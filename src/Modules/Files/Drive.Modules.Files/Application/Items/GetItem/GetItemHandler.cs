using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.GetItem
{
    internal sealed class GetItemHandler(DriveAccess driveAccess, IDriveItemRepository items)
        : IQueryHandler<GetItemQuery, DriveItemDto>
    {
        public async Task<Result<DriveItemDto>> HandleAsync(GetItemQuery query, CancellationToken cancellationToken)
        {
            var driveResult = await driveAccess.GetOwnedDriveAsync(query.DriveId, cancellationToken);
            if (driveResult.IsFailure)
            {
                return driveResult.Error;
            }

            var item = await items.GetAsync(query.DriveId, query.ItemId, cancellationToken);
            if (item is null || item.Status == ItemStatus.PendingUpload)
            {
                return FilesErrors.ItemNotFound;
            }

            return item.ToDto();
        }
    }
}
