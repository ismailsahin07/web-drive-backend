using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Folders.CreateFolder;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Folders
{
    public sealed class CreateFolderHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ValidRequest_CreatesAndSavesFolder()
        {
            var command = new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "Work");

            var result = await Handle(command);

            result.IsSuccess.ShouldBeTrue();
            var folder = _context.Store.Items.Single(item => item.Id == result.Value);
            folder.Name.Value.ShouldBe("Work");
            folder.ParentId.ShouldBe(_context.Root.Id);
            folder.Kind.ShouldBe(ItemKind.Folder);
            folder.CreatedBy.ShouldBe(_context.Owner);
            folder.CreatedAt.ShouldBe(FilesTestContext.Now);
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_InvalidName_FailsWithoutSaving()
        {
            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "a/b"));

            result.Error.ShouldBe(ItemNameErrors.InvalidCharacters);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_UnknownDrive_FailsWithDriveNotFound()
        {
            var result = await Handle(new CreateFolderCommand(DriveId.New(), _context.Root.Id, "Work"));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            _context.ActAsSomeoneElse();

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "Work"));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_UnknownParent_FailsWithItemNotFound()
        {
            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, ItemId.New(), "Work"));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_NameAlreadyUsedInFolder_FailsWithNameTaken()
        {
            _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "WORK"));

            result.Error.ShouldBe(FilesErrors.NameTaken);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_NameUsedByPendingUpload_FailsWithNameTaken()
        {
            _context.SeedPendingFile(_context.Root, "Work");

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "Work"));

            result.Error.ShouldBe(FilesErrors.NameTaken);
        }

        [Fact]
        public async Task Handle_NameUsedOnlyByTrashedItem_Succeeds()
        {
            var trashed = _context.SeedFolder(_context.Root, "Work");
            trashed.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "Work"));

            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task Handle_SameNameInDifferentFolder_Succeeds()
        {
            var other = _context.SeedFolder(_context.Root, "Other");
            _context.SeedFolder(other, "Work");

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "Work"));

            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task Handle_ParentInsideTrashedFolder_FailsWithDestinationNotAvailable()
        {
            var old = _context.SeedFolder(_context.Root, "Old");
            var sub = _context.SeedFolder(old, "Sub");
            old.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, sub.Id, "Work"));

            result.Error.ShouldBe(DriveItemErrors.DestinationNotAvailable);
        }

        [Fact]
        public async Task Handle_ParentIsTrashed_FailsWithDestinationNotAvailable()
        {
            var old = _context.SeedFolder(_context.Root, "Old");
            old.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, old.Id, "Work"));

            result.Error.ShouldBe(DriveItemErrors.DestinationNotAvailable);
        }

        [Fact]
        public async Task Handle_SaveConflict_ReturnsSaveError()
        {
            _context.Store.NextSaveError = FilesErrors.ConcurrencyConflict;

            var result = await Handle(new CreateFolderCommand(_context.Drive.Id, _context.Root.Id, "Work"));

            result.Error.ShouldBe(FilesErrors.ConcurrencyConflict);
        }

        private Task<Result<ItemId>> Handle(CreateFolderCommand command)
        {
            var handler = new CreateFolderHandler(
                _context.DriveAccess,
                _context.Store,
                _context.ItemTree,
                _context.Store,
                _context.CurrentUser,
                _context.Time);

            return handler.HandleAsync(command, TestContext.Current.CancellationToken);
        }
    }
}
