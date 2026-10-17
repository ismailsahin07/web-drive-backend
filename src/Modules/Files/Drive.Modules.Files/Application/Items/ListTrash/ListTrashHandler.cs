using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Paging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.ListTrash
{
    internal sealed class ListTrashHandler(DriveAccess driveAccess, IDriveItemRepository items)
        : IQueryHandler<ListTrashQuery, PagedResult<DriveItemDto>>
    {
        public async Task<Result<PagedResult<DriveItemDto>>> HandleAsync(ListTrashQuery query, CancellationToken cancellationToken)
        {
            var driveResult = await driveAccess.GetOwnedDriveAsync(query.DriveId, cancellationToken);
            if (driveResult.IsFailure)
            {
                return driveResult.Error;
            }

            var page = await items.ListTrashAsync(
                query.DriveId,
                PageSizes.Resolve(query.PageSize),
                query.ContinuationToken,
                cancellationToken);

            return new PagedResult<DriveItemDto>(
                page.Items.Select(item => item.ToDto()).ToList(),
                page.ContinuationToken);
        }
    }
}
