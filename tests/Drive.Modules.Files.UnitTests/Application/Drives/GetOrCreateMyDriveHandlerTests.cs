using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Drives;
using Drive.Modules.Files.Application.Drives.GetOrCreateMyDrive;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Drives
{
    public sealed class GetOrCreateMyDriveHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ExistingDrive_ReturnsItWithoutSaving()
        {
            var result = await Handle();

            result.Value.Id.ShouldBe(_context.Drive.Id.Value);
            result.Value.RootFolderId.ShouldBe(_context.Root.Id.Value);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_FirstSignIn_CreatesDriveAndRootFolder()
        {
            _context.ActAsSomeoneElse();
            var newUser = _context.CurrentUser.Id;

            var result = await Handle();

            var dto = result.Value;
            dto.Id.ShouldBe(newUser.Value);
            dto.OwnerId.ShouldBe(newUser.Value);
            dto.QuotaBytes.ShouldBe(_context.Options.DefaultQuotaBytes);
            dto.UsedBytes.ShouldBe(0);
            dto.AvailableBytes.ShouldBe(_context.Options.DefaultQuotaBytes);
            dto.CreatedAt.ShouldBe(FilesTestContext.Now);

            var root = _context.Store.Items.Single(item => item.Id.Value == dto.RootFolderId);
            root.IsRoot.ShouldBeTrue();
            root.DriveId.Value.ShouldBe(dto.Id);
            root.CreatedBy.ShouldBe(newUser);
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_CalledAgainAfterCreation_ReturnsSameDrive()
        {
            _context.ActAsSomeoneElse();
            var first = await Handle();

            var second = await Handle();

            second.Value.ShouldBe(first.Value);
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_DriveCreatedByConcurrentRequest_ReturnsThatDrive()
        {
            _context.ActAsSomeoneElse();
            var newUser = _context.CurrentUser.Id;
            var winnerRootId = ItemId.New();
            var winner = StorageDrive.Create(
                PersonalDrives.IdFor(newUser), newUser, winnerRootId, 500, FilesTestContext.Now);
            _context.Store.OnSave = () => _context.Store.Seed(winner);
            _context.Store.NextSaveError = FilesErrors.ConcurrencyConflict;

            var result = await Handle();

            result.Value.RootFolderId.ShouldBe(winnerRootId.Value);
            result.Value.QuotaBytes.ShouldBe(500);
        }

        [Fact]
        public async Task Handle_SaveFailsForOtherReason_ReturnsError()
        {
            _context.ActAsSomeoneElse();
            var failure = Error.Failure("Test.Failure", "Storage unavailable.");
            _context.Store.NextSaveError = failure;

            var result = await Handle();

            result.Error.ShouldBe(failure);
        }

        private Task<Result<StorageDriveDto>> Handle() =>
            new GetOrCreateMyDriveHandler(
                    _context.Store,
                    _context.Store,
                    _context.Store,
                    _context.CurrentUser,
                    _context.Time,
                    _context.Options)
                .HandleAsync(new GetOrCreateMyDriveCommand(), TestContext.Current.CancellationToken);
    }
}
