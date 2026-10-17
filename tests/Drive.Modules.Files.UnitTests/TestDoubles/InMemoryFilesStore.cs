using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.Domain.Items;
using Drive.SharedKernel.Results;

namespace Drive.Modules.Files.UnitTests.TestDoubles
{
    internal sealed class InMemoryFilesStore : IStorageDriveRepository, IDriveItemRepository, IUnitOfWork
    {
        private readonly Dictionary<DriveId, StorageDrive> _drives = [];
        private readonly Dictionary<ItemId, DriveItem> _items = [];
        private readonly List<StorageDrive> _pendingDrives = [];
        private readonly List<DriveItem> _pendingItems = [];

        public int SaveCount { get; private set; }

        public Error? NextSaveError { get; set; }

        public IReadOnlyCollection<DriveItem> Items => _items.Values;

        public void Seed(StorageDrive drive) => _drives[drive.Id] = drive;

        public void Seed(DriveItem item) => _items[item.Id] = item;

        public Task<StorageDrive?> GetAsync(DriveId id, CancellationToken cancellationToken) =>
            Task.FromResult<StorageDrive?>(_drives.GetValueOrDefault(id));

        public Task<DriveItem?> GetAsync(DriveId driveId, ItemId itemId, CancellationToken cancellationToken) =>
            Task.FromResult<DriveItem?>(
                _items.TryGetValue(itemId, out var item) && item.DriveId == driveId ? item : null);

        public Task<bool> NameExistsAsync(DriveId driveId, ItemId parentId, ItemName name, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Values.Any(item =>
                item.DriveId == driveId
                && item.ParentId == parentId
                && item.Status != ItemStatus.Trashed
                && item.Name.ComparisonKey == name.ComparisonKey));

        public void Add(StorageDrive drive) => _pendingDrives.Add(drive);

        public void Update(StorageDrive drive) => _pendingDrives.Add(drive);

        public void Add(DriveItem item) => _pendingItems.Add(item);

        public void Update(DriveItem item) => _pendingItems.Add(item);

        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (NextSaveError is { } error)
            {
                NextSaveError = null;
                _pendingDrives.Clear();
                _pendingItems.Clear();
                return Task.FromResult(Result.Failure(error));
            }

            foreach (var drive in _pendingDrives)
            {
                _drives[drive.Id] = drive;
            }

            foreach (var item in _pendingItems)
            {
                _items[item.Id] = item;
            }

            _pendingDrives.Clear();
            _pendingItems.Clear();
            SaveCount++;
            return Task.FromResult(Result.Success());
        }
    }
}
