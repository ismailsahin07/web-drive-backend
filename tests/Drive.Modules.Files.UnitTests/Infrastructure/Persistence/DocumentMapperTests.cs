using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.Infrastructure.Persistence;
using Drive.Modules.Files.Infrastructure.Persistence.Documents;
using Drive.Modules.Files.UnitTests.TestDoubles;

namespace Drive.Modules.Files.UnitTests.Infrastructure.Persistence
{
    public sealed class DocumentMapperTests
    {
        private readonly FilesTestContext _context = new();

        [Fact]
        public void StorageDrive_RoundTripsThroughDocument()
        {
            var drive = _context.Drive;
            drive.ReserveSpace(40);
            drive.CommitReservation(30);

            var document = DocumentMapper.ToDocument(drive);
            var restored = DocumentMapper.ToDomain(document);

            document.Id.ShouldBe("drive");
            document.Type.ShouldBe("drive");
            restored.Id.ShouldBe(drive.Id);
            restored.OwnerId.ShouldBe(drive.OwnerId);
            restored.RootFolderId.ShouldBe(drive.RootFolderId);
            restored.QuotaBytes.ShouldBe(drive.QuotaBytes);
            restored.UsedBytes.ShouldBe(30);
            restored.ReservedBytes.ShouldBe(10);
            restored.CreatedAt.ShouldBe(drive.CreatedAt);
        }

        [Fact]
        public void DriveItem_RoundTripsThroughDocument()
        {
            var file = _context.SeedAvailableFile(_context.Root, "report.pdf", 25);
            file.Trash(_context.Owner, FilesTestContext.Now.AddHours(2));

            var restored = DocumentMapper.ToDomain(DocumentMapper.ToDocument(file));

            restored.Id.ShouldBe(file.Id);
            restored.DriveId.ShouldBe(file.DriveId);
            restored.ParentId.ShouldBe(file.ParentId);
            restored.Kind.ShouldBe(ItemKind.File);
            restored.Name.ShouldBe(file.Name);
            restored.Status.ShouldBe(ItemStatus.Trashed);
            restored.SizeBytes.ShouldBe(25);
            restored.ContentType.ShouldBe(file.ContentType);
            restored.BlobName.ShouldBe(file.BlobName);
            restored.CreatedBy.ShouldBe(file.CreatedBy);
            restored.ModifiedAt.ShouldBe(file.ModifiedAt);
            restored.TrashedAt.ShouldBe(file.TrashedAt);
            restored.DomainEvents.ShouldBeEmpty();
        }

        [Fact]
        public void DriveItem_Document_StoresEnumsAsStringsWithKeys()
        {
            var folder = _context.SeedFolder(_context.Root, "Work");

            var document = DocumentMapper.ToDocument(folder);

            document.Id.ShouldBe(folder.Id.ToString());
            document.Type.ShouldBe("item");
            document.Kind.ShouldBe("Folder");
            document.Status.ShouldBe("Available");
            document.NameKey.ShouldBe($"{_context.Root.Id}/WORK");
            document.SortKey.ShouldBe("0|WORK");
        }

        [Fact]
        public void Root_Document_HasNoParentAndNoNameKey()
        {
            var document = DocumentMapper.ToDocument(_context.Root);

            document.ParentId.ShouldBeNull();
            document.NameKey.ShouldBeNull();
        }

        [Fact]
        public void Document_StoresTimesInUtc()
        {
            var istanbulTime = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.FromHours(3));
            var root = DriveItem.CreateRootFolder(_context.Root.Id, _context.Drive.Id, _context.Owner, istanbulTime);

            var document = DocumentMapper.ToDocument(root);

            document.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
            document.CreatedAt.ShouldBe(istanbulTime);
        }

        [Fact]
        public void ConcurrencyToken_IsCarriedFromDocument()
        {
            var document = DocumentMapper.ToDocument(_context.Root);

            var restored = DocumentMapper.ToDomain(new DriveItemDocument
            {
                Id = document.Id,
                DriveId = document.DriveId,
                Kind = document.Kind,
                Name = document.Name,
                SortKey = document.SortKey,
                Status = document.Status,
                CreatedBy = document.CreatedBy,
                CreatedAt = document.CreatedAt,
                ModifiedBy = document.ModifiedBy,
                ModifiedAt = document.ModifiedAt,
                ETag = "\"etag-1\"",
            });

            restored.ConcurrencyToken.ShouldBe("\"etag-1\"");
        }
    }
}
