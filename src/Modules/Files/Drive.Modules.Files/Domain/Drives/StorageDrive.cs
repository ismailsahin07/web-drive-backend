using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Domain;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.Domain.Drives
{
    internal sealed class StorageDrive : AggregateRoot<DriveId>
    {
        private StorageDrive(
            DriveId id,
            UserId ownerId,
            ItemId rootFolderId,
            long quotaBytes,
            long usedBytes,
            long reservedBytes,
            DateTimeOffset createdAt)
            : base(id)
        {
            OwnerId = ownerId;
            RootFolderId = rootFolderId;
            QuotaBytes = quotaBytes;
            UsedBytes = usedBytes;
            ReservedBytes = reservedBytes;
            CreatedAt = createdAt;
        }

        public UserId OwnerId { get; }

        public ItemId RootFolderId { get; }

        public long QuotaBytes { get; private set; }

        public long UsedBytes { get; private set; }

        public long ReservedBytes { get; private set; }

        public DateTimeOffset CreatedAt { get; }

        public long AvailableBytes => Math.Max(0, QuotaBytes - UsedBytes - ReservedBytes);

        public static StorageDrive Create(
            DriveId id,
            UserId ownerId,
            ItemId rootFolderId,
            long quotaBytes,
            DateTimeOffset now)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quotaBytes);

            return new StorageDrive(id, ownerId, rootFolderId, quotaBytes, usedBytes: 0, reservedBytes: 0, createdAt: now);
        }

        public static StorageDrive Rehydrate(
            DriveId id,
            UserId ownerId,
            ItemId rootFolderId,
            long quotaBytes,
            long usedBytes,
            long reservedBytes,
            DateTimeOffset createdAt) =>
            new(id, ownerId, rootFolderId, quotaBytes, usedBytes, reservedBytes, createdAt);

        public Result ReserveSpace(long bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);

            if (bytes > AvailableBytes)
            {
                return StorageDriveErrors.QuotaExceeded;
            }

            ReservedBytes += bytes;
            return Result.Success();
        }

        public void CommitReservation(long bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);
            EnsureReserved(bytes);

            ReservedBytes -= bytes;
            UsedBytes += bytes;
        }

        public void ReleaseReservation(long bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);
            EnsureReserved(bytes);

            ReservedBytes -= bytes;
        }

        public void ReleaseUsedSpace(long bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);

            if (bytes > UsedBytes)
            {
                throw new InvalidOperationException(
                    $"Cannot release {bytes} bytes of used space; only {UsedBytes} bytes are in use.");
            }

            UsedBytes -= bytes;
        }

        public void ChangeQuota(long quotaBytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quotaBytes);

            QuotaBytes = quotaBytes;
        }

        private void EnsureReserved(long bytes)
        {
            if (bytes > ReservedBytes)
            {
                throw new InvalidOperationException(
                    $"Cannot settle {bytes} reserved bytes; only {ReservedBytes} bytes are reserved.");
            }
        }
    }
}
