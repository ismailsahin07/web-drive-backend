using System.Text.Json;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Drive.BuildingBlocks.Infrastructure.Cosmos
{
    public static class CosmosClientFactory
    {
        public static CosmosClient Create(CosmosOptions options, Func<HttpClient>? httpClientFactory = null)
        {
            ArgumentNullException.ThrowIfNull(options);

            var endpoint = options.Endpoint
                ?? throw new InvalidOperationException("Cosmos:Endpoint is not configured.");

            var useGateway = options.UseGatewayMode || httpClientFactory is not null;

            var clientOptions = new CosmosClientOptions
            {
                ApplicationName = "web-drive",
                ConnectionMode = useGateway ? ConnectionMode.Gateway : ConnectionMode.Direct,
                LimitToEndpoint = useGateway,
                HttpClientFactory = httpClientFactory,
                UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
            };

            return string.IsNullOrWhiteSpace(options.AccountKey)
                ? new CosmosClient(endpoint, new DefaultAzureCredential(), clientOptions)
                : new CosmosClient(endpoint, options.AccountKey, clientOptions);
        }
    }
}
