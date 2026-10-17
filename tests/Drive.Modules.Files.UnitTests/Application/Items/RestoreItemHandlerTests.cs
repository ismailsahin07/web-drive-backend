using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items.RestoreItem;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class RestoreItemHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_TrashedItem_RestoresIntoOriginalFolder()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            var file = TrashedFile(work, "report.pdf");

            var result = await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            result.IsSuccess.ShouldBeTrue();
            file.Status.ShouldBe(ItemStatus.Available);
            file.TrashedAt.ShouldBeNull();
            file.ParentId.ShouldBe(work.Id);
            file.Name.Value.ShouldBe("report.pdf");
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_OriginalFolderTrashed_RestoresIntoRoot()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            var file = TrashedFile(work, "report.pdf");
            work.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            result.IsSuccess.ShouldBeTrue();
            file.ParentId.ShouldBe(_context.Root.Id);
        }

        [Fact]
        public async Task Handle_OriginalFolderInsideTrashedFolder_RestoresIntoRoot()
        {
            var old = _context.SeedFolder(_context.Root, "Old");
            var work = _context.SeedFolder(old, "Work");
            var file = TrashedFile(work, "report.pdf");
            old.Trash(_context.Owner, FilesTestContext.Now);

            await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            file.ParentId.ShouldBe(_context.Root.Id);
        }

        [Fact]
        public async Task Handle_OriginalFolderNoLongerExists_RestoresIntoRoot()
        {
            var orphan = DriveItem.Rehydrate(
                ItemId.New(), _context.Drive.Id, parentId: ItemId.New(), ItemKind.File, ItemName.Create("lost.txt").Value,
                ItemStatus.Trashed, 10, "text/plain", "blob", _context.Owner, FilesTestContext.Now,
                _context.Owner, FilesTestContext.Now, trashedAt: FilesTestContext.Now);
            _context.Store.Seed(orphan);

            var result = await Handle(new RestoreItemCommand(_context.Drive.Id, orphan.Id));

            result.IsSuccess.ShouldBeTrue();
            orphan.ParentId.ShouldBe(_context.Root.Id);
        }

        [Fact]
        public async Task Handle_NameTakenInTarget_RestoresWithNumberedName()
        {
            var file = TrashedFile(_context.Root, "report.pdf");
            _context.SeedAvailableFile(_context.Root, "report.pdf", 10);

            var result = await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            result.IsSuccess.ShouldBeTrue();
            file.Name.Value.ShouldBe("report (1).pdf");
        }

        [Fact]
        public async Task Handle_FirstNumberedNameAlsoTaken_UsesNextNumber()
        {
            var file = TrashedFile(_context.Root, "report.pdf");
            _context.SeedAvailableFile(_context.Root, "report.pdf", 10);
            _context.SeedAvailableFile(_context.Root, "report (1).pdf", 10);

            await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            file.Name.Value.ShouldBe("report (2).pdf");
        }

        [Fact]
        public async Task Handle_AvailableItem_SucceedsWithoutSaving()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 10);

            var result = await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            result.IsSuccess.ShouldBeTrue();
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_PendingUpload_FailsWithItemNotFound()
        {
            var file = _context.SeedUpload(_context.Root, "photo.jpg", 10);

            var result = await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            var file = TrashedFile(_context.Root, "report.pdf");
            _context.ActAsSomeoneElse();

            var result = await Handle(new RestoreItemCommand(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private DriveItem TrashedFile(DriveItem parent, string name)
        {
            var file = _context.SeedAvailableFile(parent, name, 10);
            file.Trash(_context.Owner, FilesTestContext.Now);
            file.PullDomainEvents();
            return file;
        }

        private Task<Result> Handle(RestoreItemCommand command) =>
            new RestoreItemHandler(
                    _context.DriveAccess,
                    _context.Store,
                    _context.ItemTree,
                    _context.NameFinder,
                    _context.Store,
                    _context.CurrentUser,
                    _context.Time)
                .HandleAsync(command, TestContext.Current.CancellationToken);
    }
}
