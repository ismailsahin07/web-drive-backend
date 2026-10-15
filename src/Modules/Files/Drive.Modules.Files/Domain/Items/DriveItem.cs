using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Domain;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Domain.Items
{
    internal sealed class DriveItem : AggregateRoot<ItemId>
    {
        private static readonly ItemName RootFolderName = ItemName.Create("My Drive").Value;

        private DriveItem(
            ItemId id,
            DriveId driveId,
            ItemId? parentId,
            ItemKind kind,
            ItemName name,
            ItemStatus status,
            long sizeBytes,
            string? contentType,
            string? blobName,
            UserId createdBy,
            DateTimeOffset createdAt,
            UserId modifiedBy,
            DateTimeOffset modifiedAt,
            DateTimeOffset? trashedAt)
            : base(id)
        {
            DriveId = driveId;
            ParentId = parentId;
            Kind = kind;
            Name = name;
            Status = status;
            SizeBytes = sizeBytes;
            ContentType = contentType;
            BlobName = blobName;
            CreatedBy = createdBy;
            CreatedAt = createdAt;
            ModifiedBy = modifiedBy;
            ModifiedAt = modifiedAt;
            TrashedAt = trashedAt;
        }

        public DriveId DriveId { get; }

        public ItemId? ParentId { get; private set; }

        public ItemKind Kind { get; }

        public ItemName Name { get; private set; }

        public ItemStatus Status { get; private set; }

        public long SizeBytes { get; }

        public string? ContentType { get; }

        public string? BlobName { get; }

        public UserId CreatedBy { get; }

        public DateTimeOffset CreatedAt { get; }

        public UserId ModifiedBy { get; private set; }

        public DateTimeOffset ModifiedAt { get; private set; }

        public DateTimeOffset? TrashedAt { get; private set; }

        public bool IsRoot => ParentId is null;

        public bool IsFolder => Kind == ItemKind.Folder;

        public bool IsAvailable => Status == ItemStatus.Available;

        public static DriveItem CreateRootFolder(ItemId id, DriveId driveId, UserId createdBy, DateTimeOffset now) =>
            new(
                id,
                driveId,
                parentId: null,
                ItemKind.Folder,
                RootFolderName,
                ItemStatus.Available,
                sizeBytes: 0,
                contentType: null,
                blobName: null,
                createdBy,
                now,
                createdBy,
                now,
                trashedAt: null);

        public static Result<DriveItem> CreateFolder(
            ItemId id,
            DriveItem parent,
            ItemName name,
            UserId createdBy,
            DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(name);

            var error = ValidateDestination(parent);
            if (error is not null)
            {
                return error;
            }

            var folder = new DriveItem(
                id,
                parent.DriveId,
                parent.Id,
                ItemKind.Folder,
                name,
                ItemStatus.Available,
                sizeBytes: 0,
                contentType: null,
                blobName: null,
                createdBy,
                now,
                createdBy,
                now,
                trashedAt: null);

            folder.Raise(new FolderCreatedDomainEvent(folder.Id, folder.DriveId, parent.Id, createdBy, now));
            return folder;
        }

        public static Result<DriveItem> CreatePendingFile(
            ItemId id,
            DriveItem parent,
            ItemName name,
            long sizeBytes,
            string contentType,
            string blobName,
            UserId createdBy,
            DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(name);
            ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

            var error = ValidateDestination(parent);
            if (error is not null)
            {
                return error;
            }

            return new DriveItem(
                id,
                parent.DriveId,
                parent.Id,
                ItemKind.File,
                name,
                ItemStatus.PendingUpload,
                sizeBytes,
                contentType,
                blobName,
                createdBy,
                now,
                createdBy,
                now,
                trashedAt: null);
        }

        public static DriveItem Rehydrate(
            ItemId id,
            DriveId driveId,
            ItemId? parentId,
            ItemKind kind,
            ItemName name,
            ItemStatus status,
            long sizeBytes,
            string? contentType,
            string? blobName,
            UserId createdBy,
            DateTimeOffset createdAt,
            UserId modifiedBy,
            DateTimeOffset modifiedAt,
            DateTimeOffset? trashedAt) =>
            new(id, driveId, parentId, kind, name, status, sizeBytes, contentType, blobName,
                createdBy, createdAt, modifiedBy, modifiedAt, trashedAt);

        public Result MarkUploaded(long actualSizeBytes, DateTimeOffset now)
        {
            if (Kind != ItemKind.File)
            {
                throw new InvalidOperationException("Only files can be marked as uploaded.");
            }

            if (Status != ItemStatus.PendingUpload)
            {
                return Result.Success();
            }

            if (actualSizeBytes != SizeBytes)
            {
                return DriveItemErrors.UploadSizeMismatch;
            }

            Status = ItemStatus.Available;
            ModifiedAt = now;
            Raise(new FileUploadedDomainEvent(Id, DriveId, SizeBytes, CreatedBy, now));
            return Result.Success();
        }

        public Result Rename(ItemName newName, UserId renamedBy, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(newName);

            var error = EnsureModifiable();
            if (error is not null)
            {
                return error;
            }

            if (newName == Name)
            {
                return Result.Success();
            }

            var oldName = Name;
            Name = newName;
            ModifiedBy = renamedBy;
            ModifiedAt = now;
            Raise(new ItemRenamedDomainEvent(Id, DriveId, oldName, newName, renamedBy, now));
            return Result.Success();
        }

        public Result MoveTo(
            DriveItem destination,
            IReadOnlyCollection<ItemId> destinationAncestorIds,
            UserId movedBy,
            DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(destinationAncestorIds);

            if (ParentId is not { } currentParentId)
            {
                return DriveItemErrors.RootCannotBeModified;
            }

            var error = EnsureModifiable() ?? ValidateDestination(destination);
            if (error is not null)
            {
                return error;
            }

            if (destination.DriveId != DriveId)
            {
                return DriveItemErrors.DifferentDrive;
            }

            if (destination.Id == Id || destinationAncestorIds.Contains(Id))
            {
                return DriveItemErrors.CannotMoveIntoItself;
            }

            if (destination.Id == currentParentId)
            {
                return Result.Success();
            }

            ParentId = destination.Id;
            Raise(new ItemMovedDomainEvent(Id, DriveId, currentParentId, destination.Id, movedBy, now));
            return Result.Success();
        }

        public Result Trash(UserId trashedBy, DateTimeOffset now)
        {
            if (IsRoot)
            {
                return DriveItemErrors.RootCannotBeModified;
            }

            if (Status == ItemStatus.Trashed)
            {
                return Result.Success();
            }

            if (Status != ItemStatus.Available)
            {
                return DriveItemErrors.NotAvailable;
            }

            Status = ItemStatus.Trashed;
            TrashedAt = now;
            Raise(new ItemTrashedDomainEvent(Id, DriveId, trashedBy, now));
            return Result.Success();
        }

        public Result Restore(UserId restoredBy, DateTimeOffset now)
        {
            if (Status == ItemStatus.Available)
            {
                return Result.Success();
            }

            if (Status != ItemStatus.Trashed)
            {
                return DriveItemErrors.NotAvailable;
            }

            Status = ItemStatus.Available;
            TrashedAt = null;
            Raise(new ItemRestoredDomainEvent(Id, DriveId, restoredBy, now));
            return Result.Success();
        }

        private static Error? ValidateDestination(DriveItem destination)
        {
            if (destination.Kind != ItemKind.Folder)
            {
                return DriveItemErrors.DestinationNotFolder;
            }

            if (destination.Status != ItemStatus.Available)
            {
                return DriveItemErrors.DestinationNotAvailable;
            }

            return null;
        }

        private Error? EnsureModifiable()
        {
            if (IsRoot)
            {
                return DriveItemErrors.RootCannotBeModified;
            }

            if (Status != ItemStatus.Available)
            {
                return DriveItemErrors.NotAvailable;
            }

            return null;
        }
    }
}
