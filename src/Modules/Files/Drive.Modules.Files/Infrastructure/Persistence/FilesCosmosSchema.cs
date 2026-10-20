using Microsoft.Azure.Cosmos;

namespace Drive.Modules.Files.Infrastructure.Persistence
{
    public static class FilesCosmosSchema
    {
        public const string ContainerName = "items";

        public const string PartitionKeyPath = "/driveId";

        public static async Task EnsureCreatedAsync(CosmosClient client, string databaseName, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(client);

            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: cancellationToken);

            await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(ContainerName, PartitionKeyPath),
                cancellationToken: cancellationToken);
        }
    }
}
