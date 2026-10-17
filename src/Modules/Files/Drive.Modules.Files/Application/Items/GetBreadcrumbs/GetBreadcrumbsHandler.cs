using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Items.GetBreadcrumbs
{
    internal sealed class GetBreadcrumbsHandler(DriveAccess driveAccess, IDriveItemRepository items, ItemTree itemTree)
        : IQueryHandler<GetBreadcrumbsQuery, IReadOnlyList<BreadcrumbDto>>
    {
        public async Task<Result<IReadOnlyList<BreadcrumbDto>>> HandleAsync(GetBreadcrumbsQuery query, CancellationToken cancellationToken)
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

            var ancestors = await itemTree.GetAncestorsAsync(item, cancellationToken);

            var breadcrumbs = ancestors
                .Reverse()
                .Append(item)
                .Select(crumb => new BreadcrumbDto(crumb.Id.Value, crumb.Name.Value))
                .ToList();

            return Result.Success<IReadOnlyList<BreadcrumbDto>>(breadcrumbs);
        }
    }
}
