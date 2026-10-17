using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items.RenameItem;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class RenameItemHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ValidName_RenamesAndSaves()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new RenameItemCommand(_context.Drive.Id, folder.Id, "Projects"));

            result.IsSuccess.ShouldBeTrue();
            folder.Name.Value.ShouldBe("Projects");
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_InvalidName_FailsWithoutSaving()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new RenameItemCommand(_context.Drive.Id, folder.Id, "a:b"));

            result.Error.ShouldBe(ItemNameErrors.InvalidCharacters);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_NameUsedBySibling_FailsWithNameTaken()
        {
            _context.SeedFolder(_context.Root, "Projects");
            var folder = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new RenameItemCommand(_context.Drive.Id, folder.Id, "projects"));

            result.Error.ShouldBe(FilesErrors.NameTaken);
            folder.Name.Value.ShouldBe("Work");
        }

        [Fact]
        public async Task Handle_CaseOnlyChange_SucceedsDespiteMatchingItself()
        {
            var folder = _context.SeedFolder(_context.Root, "work");

            var result = await Handle(new RenameItemCommand(_context.Drive.Id, folder.Id, "Work"));

            result.IsSuccess.ShouldBeTrue();
            folder.Name.Value.ShouldBe("Work");
        }

        [Fact]
        public async Task Handle_Root_FailsWithRootCannotBeModified()
        {
            var result = await Handle(new RenameItemCommand(_context.Drive.Id, _context.Root.Id, "Other"));

            result.Error.ShouldBe(DriveItemErrors.RootCannotBeModified);
        }

        [Fact]
        public async Task Handle_ItemInsideTrashedFolder_FailsWithNotAvailable()
        {
            var old = _context.SeedFolder(_context.Root, "Old");
            var folder = _context.SeedFolder(old, "Work");
            old.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new RenameItemCommand(_context.Drive.Id, folder.Id, "Projects"));

            result.Error.ShouldBe(DriveItemErrors.NotAvailable);
        }

        [Fact]
        public async Task Handle_UnknownItem_FailsWithItemNotFound()
        {
            var result = await Handle(new RenameItemCommand(_context.Drive.Id, ItemId.New(), "Projects"));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");
            _context.ActAsSomeoneElse();

            var result = await Handle(new RenameItemCommand(_context.Drive.Id, folder.Id, "Projects"));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result> Handle(RenameItemCommand command) =>
            new RenameItemHandler(
                    _context.DriveAccess,
                    _context.Store,
                    _context.ItemTree,
                    _context.Store,
                    _context.CurrentUser,
                    _context.Time)
                .HandleAsync(command, TestContext.Current.CancellationToken);
    }
}
