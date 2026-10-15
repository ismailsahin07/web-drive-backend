using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.UnitTests.Domain.Drives
{
    public sealed class StorageDriveTests
    {
        private const long Quota = 100;

        private static readonly DateTimeOffset Now = new(2026, 10, 15, 9, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Create_SetsInitialState()
        {
            var id = DriveId.New();
            var ownerId = UserId.From(Guid.NewGuid());
            var rootFolderId = ItemId.New();

            var drive = StorageDrive.Create(id, ownerId, rootFolderId, Quota, Now);

            drive.Id.ShouldBe(id);
            drive.OwnerId.ShouldBe(ownerId);
            drive.RootFolderId.ShouldBe(rootFolderId);
            drive.QuotaBytes.ShouldBe(Quota);
            drive.UsedBytes.ShouldBe(0);
            drive.ReservedBytes.ShouldBe(0);
            drive.AvailableBytes.ShouldBe(Quota);
            drive.CreatedAt.ShouldBe(Now);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        public void Create_WithNonPositiveQuota_Throws(long quota)
        {
            Should.Throw<ArgumentOutOfRangeException>(
                () => StorageDrive.Create(DriveId.New(), UserId.From(Guid.NewGuid()), ItemId.New(), quota, Now));
        }

        [Fact]
        public void Rehydrate_RestoresAllState()
        {
            var id = DriveId.New();

            var drive = StorageDrive.Rehydrate(id, UserId.From(Guid.NewGuid()), ItemId.New(), Quota, usedBytes: 40, reservedBytes: 10, Now);

            drive.Id.ShouldBe(id);
            drive.UsedBytes.ShouldBe(40);
            drive.ReservedBytes.ShouldBe(10);
            drive.AvailableBytes.ShouldBe(50);
        }

        [Fact]
        public void ReserveSpace_WithinQuota_ReservesBytes()
        {
            var drive = CreateDrive();

            var result = drive.ReserveSpace(30);

            result.IsSuccess.ShouldBeTrue();
            drive.ReservedBytes.ShouldBe(30);
            drive.UsedBytes.ShouldBe(0);
            drive.AvailableBytes.ShouldBe(70);
        }

        [Fact]
        public void ReserveSpace_ExactlyAllRemainingSpace_Succeeds()
        {
            var drive = CreateDrive();

            var result = drive.ReserveSpace(Quota);

            result.IsSuccess.ShouldBeTrue();
            drive.AvailableBytes.ShouldBe(0);
        }

        [Fact]
        public void ReserveSpace_OverQuota_FailsAndChangesNothing()
        {
            var drive = CreateDrive();

            var result = drive.ReserveSpace(Quota + 1);

            result.Error.ShouldBe(StorageDriveErrors.QuotaExceeded);
            drive.ReservedBytes.ShouldBe(0);
            drive.AvailableBytes.ShouldBe(Quota);
        }

        [Fact]
        public void ReserveSpace_CountsBothUsedAndReservedSpace()
        {
            var drive = CreateDrive();
            drive.ReserveSpace(60);
            drive.CommitReservation(60);
            drive.ReserveSpace(30);

            var result = drive.ReserveSpace(20);

            result.Error.ShouldBe(StorageDriveErrors.QuotaExceeded);
            drive.AvailableBytes.ShouldBe(10);
        }

        [Fact]
        public void ReserveSpace_ZeroBytes_SucceedsForEmptyFiles()
        {
            var drive = CreateDrive();

            var result = drive.ReserveSpace(0);

            result.IsSuccess.ShouldBeTrue();
            drive.ReservedBytes.ShouldBe(0);
        }

        [Fact]
        public void ReserveSpace_NegativeBytes_Throws()
        {
            var drive = CreateDrive();

            Should.Throw<ArgumentOutOfRangeException>(() => drive.ReserveSpace(-1));
        }

        [Fact]
        public void CommitReservation_MovesBytesFromReservedToUsed()
        {
            var drive = CreateDrive();
            drive.ReserveSpace(30);

            drive.CommitReservation(30);

            drive.ReservedBytes.ShouldBe(0);
            drive.UsedBytes.ShouldBe(30);
            drive.AvailableBytes.ShouldBe(70);
        }

        [Fact]
        public void CommitReservation_MoreThanReserved_Throws()
        {
            var drive = CreateDrive();
            drive.ReserveSpace(10);

            Should.Throw<InvalidOperationException>(() => drive.CommitReservation(11));
        }

        [Fact]
        public void ReleaseReservation_FreesReservedBytes()
        {
            var drive = CreateDrive();
            drive.ReserveSpace(30);

            drive.ReleaseReservation(30);

            drive.ReservedBytes.ShouldBe(0);
            drive.UsedBytes.ShouldBe(0);
            drive.AvailableBytes.ShouldBe(Quota);
        }

        [Fact]
        public void ReleaseReservation_MoreThanReserved_Throws()
        {
            var drive = CreateDrive();

            Should.Throw<InvalidOperationException>(() => drive.ReleaseReservation(1));
        }

        [Fact]
        public void ReleaseUsedSpace_FreesUsedBytes()
        {
            var drive = CreateDrive();
            drive.ReserveSpace(30);
            drive.CommitReservation(30);

            drive.ReleaseUsedSpace(30);

            drive.UsedBytes.ShouldBe(0);
            drive.AvailableBytes.ShouldBe(Quota);
        }

        [Fact]
        public void ReleaseUsedSpace_MoreThanUsed_Throws()
        {
            var drive = CreateDrive();

            Should.Throw<InvalidOperationException>(() => drive.ReleaseUsedSpace(1));
        }

        [Fact]
        public void ChangeQuota_BelowCurrentUsage_LeavesNoAvailableSpace()
        {
            var drive = CreateDrive();
            drive.ReserveSpace(80);
            drive.CommitReservation(80);

            drive.ChangeQuota(50);

            drive.QuotaBytes.ShouldBe(50);
            drive.AvailableBytes.ShouldBe(0);
            drive.ReserveSpace(1).Error.ShouldBe(StorageDriveErrors.QuotaExceeded);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        public void ChangeQuota_NonPositive_Throws(long quota)
        {
            var drive = CreateDrive();

            Should.Throw<ArgumentOutOfRangeException>(() => drive.ChangeQuota(quota));
        }

        private static StorageDrive CreateDrive() =>
            StorageDrive.Create(DriveId.New(), UserId.From(Guid.NewGuid()), ItemId.New(), Quota, Now);
    }
}
