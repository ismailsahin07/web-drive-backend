using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Application.Storage;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Application.Downloads.GetDownloadUrl
{
    internal sealed class GetDownloadUrlHandler(
        DriveAccess driveAccess,
        IDriveItemRepository items,
        ItemTree itemTree,
        IBlobStorage blobStorage,
        TimeProvider timeProvider,
        FilesOptions options)
        : IQueryHandler<GetDownloadUrlQuery, DownloadLink>
    {
        public async Task<Result<DownloadLink>> HandleAsync(GetDownloadUrlQuery query, CancellationToken cancellationToken)
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

            if (item.Kind != ItemKind.File)
            {
                return FilesErrors.NotAFile;
            }

            if (item.Status == ItemStatus.Trashed || await itemTree.HasTrashedAncestorAsync(item, cancellationToken))
            {
                return DriveItemErrors.NotAvailable;
            }

            var blobName = item.BlobName
                ?? throw new InvalidOperationException($"File {item.Id} has no blob name.");

            var expiresAt = timeProvider.GetUtcNow() + options.DownloadUrlLifetime;
            var url = await blobStorage.CreateDownloadUrlAsync(
                blobName,
                item.Name.Value,
                item.ContentType ?? ContentTypes.Default,
                expiresAt,
                cancellationToken);

            return new DownloadLink(url, expiresAt);
        }
    }
}
