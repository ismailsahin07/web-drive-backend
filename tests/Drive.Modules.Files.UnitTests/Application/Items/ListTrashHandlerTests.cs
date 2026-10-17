using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Application.Items.ListTrash;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Paging;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class ListTrashHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ReturnsTrashedItemsMostRecentFirst()
        {
            var older = _context.SeedFolder(_context.Root, "Older");
            var newer = _context.SeedAvailableFile(_context.Root, "newer.txt", 1);
            _context.SeedFolder(_context.Root, "StillHere");
            older.Trash(_context.Owner, FilesTestContext.Now);
            newer.Trash(_context.Owner, FilesTestContext.Now.AddMinutes(5));

            var result = await Handle(new ListTrashQuery(_context.Drive.Id));

            result.Value.Items.Select(item => item.Name).ShouldBe(new[] { "newer.txt", "Older" });
            result.Value.Items.ShouldAllBe(item => item.Status == "Trashed");
        }

        [Fact]
        public async Task Handle_EmptyTrash_ReturnsEmptyPage()
        {
            var result = await Handle(new ListTrashQuery(_context.Drive.Id));

            result.Value.Items.ShouldBeEmpty();
            result.Value.HasMore.ShouldBeFalse();
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            _context.ActAsSomeoneElse();

            var result = await Handle(new ListTrashQuery(_context.Drive.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result<PagedResult<DriveItemDto>>> Handle(ListTrashQuery query) =>
            new ListTrashHandler(_context.DriveAccess, _context.Store)
                .HandleAsync(query, TestContext.Current.CancellationToken);
    }
}
