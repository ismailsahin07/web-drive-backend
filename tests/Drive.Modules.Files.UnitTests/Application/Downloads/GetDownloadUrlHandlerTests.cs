using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Downloads.GetDownloadUrl;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Downloads
{
    public sealed class GetDownloadUrlHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_AvailableFile_ReturnsShortLivedLinkWithFileName()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 100);

            var result = await Handle(new GetDownloadUrlQuery(_context.Drive.Id, file.Id));

            var link = result.Value;
            link.Url.ToString().ShouldContain(file.BlobName!);
            link.ExpiresAt.ShouldBe(FilesTestContext.Now + _context.Options.DownloadUrlLifetime);
            _context.Blobs.LastDownloadFileName.ShouldBe("report.pdf");
            _context.Blobs.LastDownloadContentType.ShouldBe("text/plain");
        }

        [Fact]
        public async Task Handle_PendingUpload_FailsWithItemNotFound()
        {
            var file = _context.SeedUpload(_context.Root, "report.pdf", 100);

            var result = await Handle(new GetDownloadUrlQuery(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_TrashedFile_FailsWithNotAvailable()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 100);
            file.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new GetDownloadUrlQuery(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(DriveItemErrors.NotAvailable);
        }

        [Fact]
        public async Task Handle_FileInsideTrashedFolder_FailsWithNotAvailable()
        {
            var old = _context.SeedFolder(_context.Root, "Old");
            var file = _context.SeedAvailableFile(old, "report.pdf", 100);
            old.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new GetDownloadUrlQuery(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(DriveItemErrors.NotAvailable);
        }

        [Fact]
        public async Task Handle_Folder_FailsWithNotAFile()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new GetDownloadUrlQuery(_context.Drive.Id, folder.Id));

            result.Error.ShouldBe(FilesErrors.NotAFile);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 100);
            _context.ActAsSomeoneElse();

            var result = await Handle(new GetDownloadUrlQuery(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result<DownloadLink>> Handle(GetDownloadUrlQuery query) =>
            new GetDownloadUrlHandler(
                    _context.DriveAccess,
                    _context.Store,
                    _context.ItemTree,
                    _context.Blobs,
                    _context.Time,
                    _context.Options)
                .HandleAsync(query, TestContext.Current.CancellationToken);
    }
}
