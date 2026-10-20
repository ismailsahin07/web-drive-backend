using System.Net;
using Drive.Modules.Files.Application;
using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Domain.Drives;
using Drive.Modules.Files.Domain.Items;
using Drive.Modules.Files.Infrastructure.Persistence.Documents;
using Drive.SharedKernel.Paging;
using Drive.SharedKernel.Results;
using Microsoft.Azure.Cosmos;

namespace Drive.Modules.Files.Infrastructure.Persistence
{
    internal sealed class CosmosFilesStore(Container container)
        : IStorageDriveRepository, IDriveItemRepository, IUnitOfWork
    {
        private readonly Dictionary<ItemId, string?> _loadedNameKeys = [];
        private readonly List<Tracked<StorageDrive>> _drives = [];
        private readonly List<Tracked<DriveItem>> _items = [];

        private enum OperationKind
        {
            SaveAggregate,
            CreateNameClaim,
            DeleteNameClaim,
        }

        public async Task<StorageDrive?> GetAsync(DriveId id, CancellationToken cancellationToken)
        {
            var document = await ReadAsync<StorageDriveDocument>(StorageDriveDocument.FixedId, id, cancellationToken);
            return document is null ? null : DocumentMapper.ToDomain(document);
        }

        public async Task<DriveItem?> GetAsync(DriveId driveId, ItemId itemId, CancellationToken cancellationToken)
        {
            var document = await ReadAsync<DriveItemDocument>(itemId.ToString(), driveId, cancellationToken);
            if (document is null || document.Type != DocumentTypes.Item)
            {
                return null;
            }

            return Load(document);
        }

        public async Task<bool> NameExistsAsync(DriveId driveId, ItemId parentId, ItemName name, CancellationToken cancellationToken)
        {
            var claimId = NameKeys.ClaimId(NameKeys.For(parentId, name));

            using var response = await container.ReadItemStreamAsync(
                claimId,
                PartitionKeyFor(driveId),
                cancellationToken: cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }

        public Task<PagedResult<DriveItem>> ListChildrenAsync(
            DriveId driveId,
            ItemId folderId,
            int pageSize,
            string? continuationToken,
            CancellationToken cancellationToken)
        {
            var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.type = @type AND c.parentId = @parentId AND c.status = @status ORDER BY c.sortKey")
                .WithParameter("@type", DocumentTypes.Item)
                .WithParameter("@parentId", folderId.Value)
                .WithParameter("@status", nameof(ItemStatus.Available));

            return QueryPageAsync(query, driveId, pageSize, continuationToken, cancellationToken);
        }

        public Task<PagedResult<DriveItem>> ListTrashAsync(
            DriveId driveId,
            int pageSize,
            string? continuationToken,
            CancellationToken cancellationToken)
        {
            var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.type = @type AND c.status = @status ORDER BY c.trashedAt DESC")
                .WithParameter("@type", DocumentTypes.Item)
                .WithParameter("@status", nameof(ItemStatus.Trashed));

            return QueryPageAsync(query, driveId, pageSize, continuationToken, cancellationToken);
        }

        public void Add(StorageDrive drive) => Track(_drives, drive, isNew: true);

        public void Update(StorageDrive drive) => Track(_drives, drive, isNew: false);

        public void Add(DriveItem item) => Track(_items, item, isNew: true);

        public void Update(DriveItem item) => Track(_items, item, isNew: false);

        public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (_drives.Count == 0 && _items.Count == 0)
            {
                return Result.Success();
            }

            var batch = container.CreateTransactionalBatch(PartitionKeyFor(GetSinglePartition()));
            var operations = new List<BatchOperation>();

            foreach (var (drive, isNew) in _drives)
            {
                var document = DocumentMapper.ToDocument(drive);
                if (isNew)
                {
                    batch.CreateItem(document);
                }
                else
                {
                    batch.ReplaceItem(document.Id, document, IfMatch(drive.ConcurrencyToken));
                }

                operations.Add(new BatchOperation(OperationKind.SaveAggregate, drive));
            }

            foreach (var (item, isNew) in _items)
            {
                var document = DocumentMapper.ToDocument(item);
                if (isNew)
                {
                    batch.CreateItem(document);
                }
                else
                {
                    batch.ReplaceItem(document.Id, document, IfMatch(item.ConcurrencyToken));
                }

                operations.Add(new BatchOperation(OperationKind.SaveAggregate, item));

                var previousNameKey = isNew ? null : _loadedNameKeys.GetValueOrDefault(item.Id);
                var currentNameKey = document.NameKey;

                if (previousNameKey == currentNameKey)
                {
                    continue;
                }

                if (previousNameKey is not null)
                {
                    batch.DeleteItem(NameKeys.ClaimId(previousNameKey));
                    operations.Add(new BatchOperation(OperationKind.DeleteNameClaim, Aggregate: null));
                }

                if (currentNameKey is not null)
                {
                    batch.CreateItem(new NameClaimDocument
                    {
                        Id = NameKeys.ClaimId(currentNameKey),
                        DriveId = item.DriveId.Value,
                        ItemId = item.Id.Value,
                        NameKey = currentNameKey,
                    });
                    operations.Add(new BatchOperation(OperationKind.CreateNameClaim, Aggregate: null));
                }
            }

            using var response = await batch.ExecuteAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ToError(response, operations);
            }

            for (var index = 0; index < operations.Count; index++)
            {
                switch (operations[index].Aggregate)
                {
                    case StorageDrive drive:
                        drive.SetConcurrencyToken(response[index].ETag);
                        break;
                    case DriveItem item:
                        item.SetConcurrencyToken(response[index].ETag);
                        _loadedNameKeys[item.Id] = NameKeys.For(item);
                        break;
                }
            }

            _drives.Clear();
            _items.Clear();
            return Result.Success();
        }

        private static Error ToError(TransactionalBatchResponse response, List<BatchOperation> operations)
        {
            var failedIndex = Enumerable.Range(0, response.Count)
                .FirstOrDefault(index => response[index].StatusCode != HttpStatusCode.FailedDependency, -1);

            var failedKind = failedIndex >= 0 ? operations[failedIndex].Kind : (OperationKind?)null;

            return (response.StatusCode, failedKind) switch
            {
                (HttpStatusCode.Conflict, OperationKind.CreateNameClaim) => FilesErrors.NameTaken,
                (HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed or HttpStatusCode.NotFound, _) => FilesErrors.ConcurrencyConflict,
                _ => throw new InvalidOperationException(
                    $"Saving changes failed with status {(int)response.StatusCode}: {response.ErrorMessage}"),
            };
        }

        private static TransactionalBatchItemRequestOptions IfMatch(string? concurrencyToken) => new()
        {
            IfMatchEtag = concurrencyToken
                ?? throw new InvalidOperationException("An updated aggregate must have been loaded from the database first."),
        };

        private static PartitionKey PartitionKeyFor(DriveId driveId) => new(driveId.ToString());

        private static void Track<T>(List<Tracked<T>> tracked, T aggregate, bool isNew)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(aggregate);

            if (!tracked.Exists(entry => ReferenceEquals(entry.Aggregate, aggregate)))
            {
                tracked.Add(new Tracked<T>(aggregate, isNew));
            }
        }

        private DriveId GetSinglePartition()
        {
            var partitions = _drives.Select(entry => entry.Aggregate.Id)
                .Concat(_items.Select(entry => entry.Aggregate.DriveId))
                .Distinct()
                .ToList();

            return partitions.Count == 1
                ? partitions[0]
                : throw new InvalidOperationException("A unit of work can only save changes within a single drive.");
        }

        private DriveItem Load(DriveItemDocument document)
        {
            var item = DocumentMapper.ToDomain(document);
            _loadedNameKeys[item.Id] = document.NameKey;
            return item;
        }

        private async Task<T?> ReadAsync<T>(string id, DriveId driveId, CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                var response = await container.ReadItemAsync<T>(id, PartitionKeyFor(driveId), cancellationToken: cancellationToken);
                return response.Resource;
            }
            catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private async Task<PagedResult<DriveItem>> QueryPageAsync(
            QueryDefinition query,
            DriveId driveId,
            int pageSize,
            string? continuationToken,
            CancellationToken cancellationToken)
        {
            using var iterator = container.GetItemQueryIterator<DriveItemDocument>(
                query,
                continuationToken,
                new QueryRequestOptions
                {
                    PartitionKey = PartitionKeyFor(driveId),
                    MaxItemCount = pageSize,
                });

            if (!iterator.HasMoreResults)
            {
                return new PagedResult<DriveItem>([], ContinuationToken: null);
            }

            var page = await iterator.ReadNextAsync(cancellationToken);
            var items = page.Select(Load).ToList();

            return new PagedResult<DriveItem>(items, page.ContinuationToken);
        }

        private sealed record Tracked<T>(T Aggregate, bool IsNew);

        private sealed record BatchOperation(OperationKind Kind, object? Aggregate);
    }
}
