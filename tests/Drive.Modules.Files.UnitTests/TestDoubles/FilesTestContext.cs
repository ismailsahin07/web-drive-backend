using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Application.Storage;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Domain;

namespace Drive.Modules.Files.UnitTests.TestDoubles
{
    internal sealed class FilesTestContext
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

        public FilesTestContext()
        {
            Owner = UserId.From(Guid.NewGuid());
            CurrentUser = new FakeCurrentUser(Owner);

            var driveId = DriveId.New();
            var rootId = ItemId.New();
            Drive = StorageDrive.Create(driveId, Owner, rootId, quotaBytes: 1_000, Now);
            Root = DriveItem.CreateRootFolder(rootId, driveId, Owner, Now);

            Store.Seed(Drive);
            Store.Seed(Root);
        }

        public UserId Owner { get; }

        public FakeCurrentUser CurrentUser { get; set; }

        public InMemoryFilesStore Store { get; } = new();

        public FixedTimeProvider Time { get; } = new(Now);

        public FakeBlobStorage Blobs { get; } = new();

        public FilesOptions Options { get; } = new()
        {
            MaxFileSizeBytes = 800,
            UploadUrlLifetime = TimeSpan.FromHours(1),
            DownloadUrlLifetime = TimeSpan.FromMinutes(10),
        };

        public StorageDrive Drive { get; }

        public DriveItem Root { get; }

        public DriveAccess DriveAccess => new(Store, CurrentUser);

        public ItemTree ItemTree => new(Store);

        public void ActAsSomeoneElse() => CurrentUser = new FakeCurrentUser(UserId.From(Guid.NewGuid()));

        public DriveItem SeedFolder(DriveItem parent, string name)
        {
            var folder = DriveItem.CreateFolder(ItemId.New(), parent, ItemName.Create(name).Value, Owner, Now).Value;
            folder.PullDomainEvents();
            Store.Seed(folder);
            return folder;
        }

        public DriveItem SeedPendingFile(DriveItem parent, string name)
        {
            var file = DriveItem.CreatePendingFile(
                ItemId.New(), parent, ItemName.Create(name).Value, 10, "application/octet-stream", "blob", Owner, Now).Value;
            Store.Seed(file);
            return file;
        }

        public DriveItem SeedUpload(DriveItem parent, string name, long sizeBytes)
        {
            var id = ItemId.New();
            Drive.ReserveSpace(sizeBytes);
            var file = DriveItem.CreatePendingFile(
                id, parent, ItemName.Create(name).Value, sizeBytes, "text/plain", BlobNames.ForNewFile(Drive.Id, id), Owner, Now).Value;
            Store.Seed(file);
            return file;
        }

        public DriveItem SeedAvailableFile(DriveItem parent, string name, long sizeBytes)
        {
            var file = SeedUpload(parent, name, sizeBytes);
            file.MarkUploaded(sizeBytes, Now);
            Drive.CommitReservation(sizeBytes);
            file.PullDomainEvents();
            return file;
        }
    }
}
