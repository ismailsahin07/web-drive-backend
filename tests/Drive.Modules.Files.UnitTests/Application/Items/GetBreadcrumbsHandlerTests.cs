using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items.GetBreadcrumbs;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class GetBreadcrumbsHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_NestedItem_ReturnsPathFromRootToItem()
        {
            var work = _context.SeedFolder(_context.Root, "Work");
            var reports = _context.SeedFolder(work, "Reports");
            var file = _context.SeedAvailableFile(reports, "q3.pdf", 1);

            var result = await Handle(new GetBreadcrumbsQuery(_context.Drive.Id, file.Id));

            result.Value.Select(crumb => crumb.Name).ShouldBe(new[] { "My Drive", "Work", "Reports", "q3.pdf" });
            result.Value[0].Id.ShouldBe(_context.Root.Id.Value);
            result.Value[^1].Id.ShouldBe(file.Id.Value);
        }

        [Fact]
        public async Task Handle_Root_ReturnsOnlyRoot()
        {
            var result = await Handle(new GetBreadcrumbsQuery(_context.Drive.Id, _context.Root.Id));

            result.Value.ShouldHaveSingleItem().Name.ShouldBe("My Drive");
        }

        [Fact]
        public async Task Handle_PendingUpload_FailsWithItemNotFound()
        {
            var file = _context.SeedUpload(_context.Root, "photo.jpg", 1);

            var result = await Handle(new GetBreadcrumbsQuery(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            _context.ActAsSomeoneElse();

            var result = await Handle(new GetBreadcrumbsQuery(_context.Drive.Id, _context.Root.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result<IReadOnlyList<BreadcrumbDto>>> Handle(GetBreadcrumbsQuery query) =>
            new GetBreadcrumbsHandler(_context.DriveAccess, _context.Store, _context.ItemTree)
                .HandleAsync(query, TestContext.Current.CancellationToken);
    }
}
