using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items.TrashItem;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class TrashItemHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_AvailableItem_TrashesAndSaves()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 100);

            var result = await Handle(new TrashItemCommand(_context.Drive.Id, file.Id));

            result.IsSuccess.ShouldBeTrue();
            file.Status.ShouldBe(ItemStatus.Trashed);
            file.TrashedAt.ShouldBe(FilesTestContext.Now);
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_TrashedFile_KeepsCountingTowardQuota()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 100);

            await Handle(new TrashItemCommand(_context.Drive.Id, file.Id));

            _context.Drive.UsedBytes.ShouldBe(100);
        }

        [Fact]
        public async Task Handle_AlreadyTrashed_SucceedsWithoutSaving()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 100);
            file.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new TrashItemCommand(_context.Drive.Id, file.Id));

            result.IsSuccess.ShouldBeTrue();
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_Root_FailsWithRootCannotBeModified()
        {
            var result = await Handle(new TrashItemCommand(_context.Drive.Id, _context.Root.Id));

            result.Error.ShouldBe(DriveItemErrors.RootCannotBeModified);
        }

        [Fact]
        public async Task Handle_PendingUpload_FailsWithItemNotFound()
        {
            var file = _context.SeedUpload(_context.Root, "photo.jpg", 100);

            var result = await Handle(new TrashItemCommand(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 100);
            _context.ActAsSomeoneElse();

            var result = await Handle(new TrashItemCommand(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result> Handle(TrashItemCommand command) =>
            new TrashItemHandler(
                    _context.DriveAccess,
                    _context.Store,
                    _context.Store,
                    _context.CurrentUser,
                    _context.Time)
                .HandleAsync(command, TestContext.Current.CancellationToken);
    }
}
