using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Uploads;
using Drive.Modules.Files.Application.Uploads.CompleteUpload;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Uploads
{
    public sealed class CompleteUploadHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_UploadedBlobWithDeclaredSize_MakesFileAvailableAndCommitsQuota()
        {
            var file = _context.SeedUpload(_context.Root, "report.pdf", 300);
            _context.Blobs.SimulateUpload(file.BlobName!, 300);

            var result = await Handle(new CompleteUploadCommand(_context.Drive.Id, file.Id));

            result.IsSuccess.ShouldBeTrue();
            file.Status.ShouldBe(ItemStatus.Available);
            _context.Drive.ReservedBytes.ShouldBe(0);
            _context.Drive.UsedBytes.ShouldBe(300);
            _context.Store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_BlobNotUploadedYet_FailsWithNotUploaded()
        {
            var file = _context.SeedUpload(_context.Root, "report.pdf", 300);

            var result = await Handle(new CompleteUploadCommand(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(UploadErrors.NotUploaded);
            file.Status.ShouldBe(ItemStatus.PendingUpload);
            _context.Drive.ReservedBytes.ShouldBe(300);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_BlobSizeDiffersFromDeclared_FailsAndKeepsReservation()
        {
            var file = _context.SeedUpload(_context.Root, "report.pdf", 300);
            _context.Blobs.SimulateUpload(file.BlobName!, 299);

            var result = await Handle(new CompleteUploadCommand(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(DriveItemErrors.UploadSizeMismatch);
            file.Status.ShouldBe(ItemStatus.PendingUpload);
            _context.Drive.ReservedBytes.ShouldBe(300);
            _context.Drive.UsedBytes.ShouldBe(0);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_AlreadyCompleted_SucceedsWithoutSavingAgain()
        {
            var file = _context.SeedUpload(_context.Root, "report.pdf", 300);
            _context.Blobs.SimulateUpload(file.BlobName!, 300);
            await Handle(new CompleteUploadCommand(_context.Drive.Id, file.Id));

            var second = await Handle(new CompleteUploadCommand(_context.Drive.Id, file.Id));

            second.IsSuccess.ShouldBeTrue();
            _context.Store.SaveCount.ShouldBe(1);
            _context.Drive.UsedBytes.ShouldBe(300);
        }

        [Fact]
        public async Task Handle_Folder_FailsWithNotAFile()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");

            var result = await Handle(new CompleteUploadCommand(_context.Drive.Id, folder.Id));

            result.Error.ShouldBe(FilesErrors.NotAFile);
        }

        [Fact]
        public async Task Handle_UnknownItem_FailsWithItemNotFound()
        {
            var result = await Handle(new CompleteUploadCommand(_context.Drive.Id, ItemId.New()));

            result.Error.ShouldBe(FilesErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            var file = _context.SeedUpload(_context.Root, "report.pdf", 300);
            _context.ActAsSomeoneElse();

            var result = await Handle(new CompleteUploadCommand(_context.Drive.Id, file.Id));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        private Task<Result> Handle(CompleteUploadCommand command)
        {
            var handler = new CompleteUploadHandler(
                _context.DriveAccess,
                _context.Store,
                _context.Store,
                _context.Store,
                _context.Blobs,
                _context.Time);

            return handler.HandleAsync(command, TestContext.Current.CancellationToken);
        }
    }
}
