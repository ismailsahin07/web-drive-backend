using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Paging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.ListChildren
{
    internal sealed class ListChildrenHandler(DriveAccess driveAccess, IDriveItemRepository items)
        : IQueryHandler<ListChildrenQuery, PagedResult<DriveItemDto>>
    {
        public async Task<Result<PagedResult<DriveItemDto>>> HandleAsync(ListChildrenQuery query, CancellationToken cancellationToken)
        {
            var driveResult = await driveAccess.GetOwnedDriveAsync(query.DriveId, cancellationToken);
            if (driveResult.IsFailure)
            {
                return driveResult.Error;
            }

            var folder = await items.GetAsync(query.DriveId, query.FolderId, cancellationToken);
            if (folder is null || folder.Status == ItemStatus.PendingUpload)
            {
                return FilesErrors.ItemNotFound;
            }

            if (folder.Kind != ItemKind.Folder)
            {
                return FilesErrors.NotAFolder;
            }

            var page = await items.ListChildrenAsync(
                query.DriveId,
                folder.Id,
                PageSizes.Resolve(query.PageSize),
                query.ContinuationToken,
                cancellationToken);

            return new PagedResult<DriveItemDto>(
                page.Items.Select(item => item.ToDto()).ToList(),
                page.ContinuationToken);
        }
    }
}
