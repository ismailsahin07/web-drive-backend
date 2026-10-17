using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Application.Items.GetItem;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Items
{
    public sealed class GetItemHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ExistingItem_ReturnsMappedDto()
        {
            var work = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new GetItemQuery(_context.Drive.Id, work.Id));

            var dto = result.Value;
            dto.Id.ShouldBe(work.Id.Value);
            dto.DriveId.ShouldBe(_context.Drive.Id.Value);
            dto.ParentId.ShouldBe(_context.Root.Id.Value);
            dto.Kind.ShouldBe("Folder");
            dto.Name.ShouldBe("Work");
            dto.Status.ShouldBe("Available");
            dto.CreatedBy.ShouldBe(_context.Owner.Value);
            dto.TrashedAt.ShouldBeNull();
        }

        [Fact]
        public async Task Handle_Root_ReturnsDtoWithoutParent()
        {
            var result = await Handle(new GetItemQuery(_context.Drive.Id, _context.Root.Id));

            result.Value.ParentId.ShouldBeNull();
        }

        [Fact]
        public async Task Handle_PendingUpload_FailsWithItemNotFound()
        {
            var pending = _context.SeedPendingFile(_context.Root, "photo.jpg");

            var result = await Handle(new GetItemQuery(_context.Drive.Id, pending.Id));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_UnknownItem_FailsWithItemNotFound()
        {
            var result = await Handle(new GetItemQuery(_context.Drive.Id, ItemId.New()));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            _context.ActAsSomeoneElse();

            var result = await Handle(new GetItemQuery(_context.Drive.Id, _context.Root.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result<DriveItemDto>> Handle(GetItemQuery query) =>
            new GetItemHandler(_context.DriveAccess, _context.Store)
                .HandleAsync(query, TestContext.Current.CancellationToken);
    }
}
