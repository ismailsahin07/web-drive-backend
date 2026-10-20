using System.Globalization;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.Infrastructure.Persistence.Documents;
using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.Infrastructure.Persistence
{
    internal static class DocumentMapper
    {
        public static StorageDriveDocument ToDocument(StorageDrive drive)
        {
            ArgumentNullException.ThrowIfNull(drive);

            return new StorageDriveDocument
            {
                DriveId = drive.Id.Value,
                OwnerId = drive.OwnerId.Value,
                RootFolderId = drive.RootFolderId.Value,
                QuotaBytes = drive.QuotaBytes,
                UsedBytes = drive.UsedBytes,
                ReservedBytes = drive.ReservedBytes,
                CreatedAt = drive.CreatedAt.ToUniversalTime(),
            };
        }

        public static StorageDrive ToDomain(StorageDriveDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            var drive = StorageDrive.Rehydrate(
                DriveId.From(document.DriveId),
                UserId.From(document.OwnerId),
                ItemId.From(document.RootFolderId),
                document.QuotaBytes,
                document.UsedBytes,
                document.ReservedBytes,
                document.CreatedAt);

            drive.SetConcurrencyToken(document.ETag);
            return drive;
        }

        public static DriveItemDocument ToDocument(DriveItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return new DriveItemDocument
            {
                Id = item.Id.ToString(),
                DriveId = item.DriveId.Value,
                ParentId = item.ParentId?.Value,
                Kind = item.Kind.ToString(),
                Name = item.Name.Value,
                NameKey = NameKeys.For(item),
                SortKey = NameKeys.SortKey(item),
                Status = item.Status.ToString(),
                SizeBytes = item.SizeBytes,
                ContentType = item.ContentType,
                BlobName = item.BlobName,
                CreatedBy = item.CreatedBy.Value,
                CreatedAt = item.CreatedAt.ToUniversalTime(),
                ModifiedBy = item.ModifiedBy.Value,
                ModifiedAt = item.ModifiedAt.ToUniversalTime(),
                TrashedAt = item.TrashedAt?.ToUniversalTime(),
            };
        }

        public static DriveItem ToDomain(DriveItemDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            var item = DriveItem.Rehydrate(
                ItemId.From(Guid.Parse(document.Id, CultureInfo.InvariantCulture)),
                DriveId.From(document.DriveId),
                document.ParentId is { } parentId ? ItemId.From(parentId) : null,
                Enum.Parse<ItemKind>(document.Kind),
                ItemName.Rehydrate(document.Name),
                Enum.Parse<ItemStatus>(document.Status),
                document.SizeBytes,
                document.ContentType,
                document.BlobName,
                UserId.From(document.CreatedBy),
                document.CreatedAt,
                UserId.From(document.ModifiedBy),
                document.ModifiedAt,
                document.TrashedAt);

            item.SetConcurrencyToken(document.ETag);
            return item;
        }
    }
}
