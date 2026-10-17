using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Application.Items.ListChildren;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Paging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class ListChildrenHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ReturnsFoldersFirstThenNamesIgnoringCase()
        {
            _context.SeedAvailableFile(_context.Root, "b.txt", 1);
            _context.SeedFolder(_context.Root, "Zeta");
            _context.SeedAvailableFile(_context.Root, "A.txt", 1);
            _context.SeedFolder(_context.Root, "alpha");

            var result = await Handle(new ListChildrenQuery(_context.Drive.Id, _context.Root.Id));

            result.Value.Items.Select(item => item.Name).ShouldBe(new[] { "alpha", "Zeta", "A.txt", "b.txt" });
            result.Value.HasMore.ShouldBeFalse();
        }

        [Fact]
        public async Task Handle_ExcludesTrashedPendingAndNestedItems()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            _context.SeedFolder(work, "Nested");
            _context.SeedUpload(_context.Root, "uploading.bin", 1);
            var trashed = _context.SeedFolder(_context.Root, "Old");
            trashed.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new ListChildrenQuery(_context.Drive.Id, _context.Root.Id));

            result.Value.Items.Select(item => item.Name).ShouldBe(new[] { "Work" });
        }

        [Fact]
        public async Task Handle_PagesThroughAllChildrenWithContinuationTokens()
        {
            foreach (var name in new[] { "a", "b", "c", "d", "e" })
            {
                _context.SeedFolder(_context.Root, name);
            }

            var first = await Handle(new ListChildrenQuery(_context.Drive.Id, _context.Root.Id, PageSize: 2));
            var second = await Handle(new ListChildrenQuery(_context.Drive.Id, _context.Root.Id, 2, first.Value.ContinuationToken));
            var third = await Handle(new ListChildrenQuery(_context.Drive.Id, _context.Root.Id, 2, second.Value.ContinuationToken));

            first.Value.Items.Select(item => item.Name).ShouldBe(new[] { "a", "b" });
            second.Value.Items.Select(item => item.Name).ShouldBe(new[] { "c", "d" });
            third.Value.Items.Select(item => item.Name).ShouldBe(new[] { "e" });
            first.Value.HasMore.ShouldBeTrue();
            second.Value.HasMore.ShouldBeTrue();
            third.Value.HasMore.ShouldBeFalse();
        }

        [Fact]
        public async Task Handle_EmptyFolder_ReturnsEmptyPage()
        {
            var work = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new ListChildrenQuery(_context.Drive.Id, work.Id));

            result.Value.Items.ShouldBeEmpty();
            result.Value.ContinuationToken.ShouldBeNull();
        }

        [Fact]
        public async Task Handle_File_FailsWithNotAFolder()
        {
            var file = _context.SeedAvailableFile(_context.Root, "a.txt", 1);

            var result = await Handle(new ListChildrenQuery(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.NotAFolder);
        }

        [Fact]
        public async Task Handle_UnknownFolder_FailsWithItemNotFound()
        {
            var result = await Handle(new ListChildrenQuery(_context.Drive.Id, ItemId.New()));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            _context.ActAsSomeoneElse();

            var result = await Handle(new ListChildrenQuery(_context.Drive.Id, _context.Root.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result<PagedResult<DriveItemDto>>> Handle(ListChildrenQuery query) =>
            new ListChildrenHandler(_context.DriveAccess, _context.Store)
                .HandleAsync(query, TestContext.Current.CancellationToken);
    }
}
