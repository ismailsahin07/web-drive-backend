using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Uploads;
using Drive.Modules.Files.Application.Uploads.InitiateUpload;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.Application.Uploads
{
    public sealed class InitiateUploadHandlerTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public async Task Handle_ValidRequest_CreatesPendingFileReservesQuotaAndReturnsTicket()
        {
            var result = await Handle(Command("report.pdf", 300, "application/pdf"));

            var ticket = result.Value;
            var file = _context.Store.Items.Single(item => item.Id.Value == ticket.ItemId);
            file.Status.ShouldBe(ItemStatus.PendingUpload);
            file.Name.Value.ShouldBe("report.pdf");
            file.SizeBytes.ShouldBe(300);
            file.ContentType.ShouldBe("application/pdf");
            file.ParentId.ShouldBe(_context.Root.Id);
            file.BlobName.ShouldStartWith($"{_context.Drive.Id}/{file.Id}/");

            _context.Drive.ReservedBytes.ShouldBe(300);
            _context.Drive.UsedBytes.ShouldBe(0);
            _context.Store.SaveCount.ShouldBe(1);

            ticket.ExpiresAt.ShouldBe(FilesTestContext.Now + _context.Options.UploadUrlLifetime);
            ticket.UploadUrl.ToString().ShouldContain(file.BlobName!);
            _context.Blobs.UploadUrlsIssued.ShouldHaveSingleItem().ShouldBe(file.BlobName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_MissingContentType_DefaultsToOctetStream(string? contentType)
        {
            var result = await Handle(Command("data.bin", 10, contentType));

            var file = _context.Store.Items.Single(item => item.Id.Value == result.Value.ItemId);
            file.ContentType.ShouldBe("application/octet-stream");
        }

        [Fact]
        public async Task Handle_EmptyFile_IsAllowed()
        {
            var result = await Handle(Command(".gitkeep", 0, "text/plain"));

            result.IsSuccess.ShouldBeTrue();
            _context.Drive.ReservedBytes.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_InvalidName_FailsWithoutSaving()
        {
            var result = await Handle(Command("CON", 10, "text/plain"));

            result.Error.ShouldBe(ItemNameErrors.Reserved);
            _context.Store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_NegativeSize_FailsWithInvalidSize()
        {
            var result = await Handle(Command("a.txt", -1, "text/plain"));

            result.Error.ShouldBe(UploadErrors.InvalidSize);
        }

        [Fact]
        public async Task Handle_FileOverMaxSize_FailsWithFileTooLarge()
        {
            var result = await Handle(Command("big.iso", _context.Options.MaxFileSizeBytes + 1, null));

            result.Error.ShouldBe(UploadErrors.FileTooLarge);
        }

        [Fact]
        public async Task Handle_NotEnoughQuota_FailsWithoutReservingOrSaving()
        {
            _context.SeedAvailableFile(_context.Root, "existing.bin", 700);

            var result = await Handle(Command("new.bin", 301, null));

            result.Error.ShouldBe(StorageDriveErrors.QuotaExceeded);
            _context.Drive.ReservedBytes.ShouldBe(0);
            _context.Store.SaveCount.ShouldBe(0);
            _context.Blobs.UploadUrlsIssued.ShouldBeEmpty();
        }

        [Fact]
        public async Task Handle_NameAlreadyUsed_FailsWithNameTaken()
        {
            _context.SeedAvailableFile(_context.Root, "report.pdf", 10);

            var result = await Handle(Command("Report.PDF", 10, null));

            result.Error.ShouldBe(FilesErrors.NameTaken);
        }

        [Fact]
        public async Task Handle_SomeoneElsesDrive_FailsWithDriveNotFound()
        {
            _context.ActAsSomeoneElse();

            var result = await Handle(Command("a.txt", 10, null));

            result.Error.ShouldBe(FilesErrors.DriveNotFound);
        }

        [Fact]
        public async Task Handle_ParentInsideTrashedFolder_FailsWithDestinationNotAvailable()
        {
            var old = _context.SeedFolder(_context.Root, "Old");
            var sub = _context.SeedFolder(old, "Sub");
            old.Trash(_context.Owner, FilesTestContext.Now);

            var result = await Handle(new InitiateUploadCommand(_context.Drive.Id, sub.Id, "a.txt", 10, null));

            result.Error.ShouldBe(DriveItemErrors.DestinationNotAvailable);
        }

        [Fact]
        public async Task Handle_SaveFails_DoesNotIssueUploadUrl()
        {
            _context.Store.NextSaveError = FilesErrors.ConcurrencyConflict;

            var result = await Handle(Command("a.txt", 10, null));

            result.Error.ShouldBe(FilesErrors.ConcurrencyConflict);
            _context.Blobs.UploadUrlsIssued.ShouldBeEmpty();
        }

        private InitiateUploadCommand Command(string fileName, long sizeBytes, string? contentType) =>
            new(_context.Drive.Id, _context.Root.Id, fileName, sizeBytes, contentType);

        private Task<Result<UploadTicket>> Handle(InitiateUploadCommand command)
        {
            var handler = new InitiateUploadHandler(
                _context.DriveAccess,
                _context.Store,
                _context.Store,
                _context.ItemTree,
                _context.Store,
                _context.Blobs,
                _context.CurrentUser,
                _context.Time,
                _context.Options);

            return handler.HandleAsync(command, TestContext.Current.CancellationToken);
        }
    }
}
