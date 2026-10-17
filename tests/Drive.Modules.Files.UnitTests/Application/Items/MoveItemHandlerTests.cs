using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items.MoveItem;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class MoveItemHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ValidMove_ChangesParentAndSaves()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            var archive = _context.SeedFolder(_context.Root, "Archive");

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, archive.Id));

            result.IsSuccess.ShouldBeTrue();
            work.ParentId.ShouldBe(archive.Id);
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_IntoOwnSubfolder_FailsWithCannotMoveIntoItself()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            var reports = _context.SeedFolder(work, "Reports");
            var y2026 = _context.SeedFolder(reports, "2026");

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, y2026.Id));

            result.Error.ShouldBe(DriveItemErrors.CannotMoveIntoItself);
            work.ParentId.ShouldBe(_context.Root.Id);
        }

        [Fact]
        public async Task Handle_NameAlreadyUsedInDestination_FailsWithNameTaken()
        {
            var archive = _context.SeedFolder(_context.Root, "Archive");
            _context.SeedFolder(archive, "Work");
            var work = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, archive.Id));

            result.Error.ShouldBe(FilesErrors.NameTaken);
            work.ParentId.ShouldBe(_context.Root.Id);
        }

        [Fact]
        public async Task Handle_ToCurrentParent_SucceedsWithoutNameConflict()
        {
            var work = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, _context.Root.Id));

            result.IsSuccess.ShouldBeTrue();
            work.ParentId.ShouldBe(_context.Root.Id);
        }

        [Fact]
        public async Task Handle_DestinationInsideTrashedFolder_FailsWithDestinationNotAvailable()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            var old = _context.SeedFolder(_context.Root, "Old");
            var sub = _context.SeedFolder(old, "Sub");
            old.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, sub.Id));

            result.Error.ShouldBe(DriveItemErrors.DestinationNotAvailable);
        }

        [Fact]
        public async Task Handle_DestinationIsFile_FailsWithDestinationNotFolder()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            var file = _context.SeedAvailableFile(_context.Root, "a.txt", 10);

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, file.Id));

            result.Error.ShouldBe(DriveItemErrors.DestinationNotFolder);
        }

        [Fact]
        public async Task Handle_ItemInsideTrashedFolder_FailsWithNotAvailable()
        {
            var old = _context.SeedFolder(_context.Root, "Old");
            var work = _context.SeedFolder(old, "Work");
            old.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, _context.Root.Id));

            result.Error.ShouldBe(DriveItemErrors.NotAvailable);
        }

        [Fact]
        public async Task Handle_UnknownDestination_FailsWithItemNotFound()
        {
            var work = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new MoveItemCommand(_context.Drive.Id, work.Id, ItemId.New()));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        private Task<Result> Handle(MoveItemCommand command) =>
            new MoveItemHandler(
                    _context.DriveAccess,
                    _context.Store,
                    _context.ItemTree,
                    _context.Store,
                    _context.CurrentUser,
                    _context.Time)
                .HandleAsync(command, TestContext.Current.CancellationToken);
    }
}
