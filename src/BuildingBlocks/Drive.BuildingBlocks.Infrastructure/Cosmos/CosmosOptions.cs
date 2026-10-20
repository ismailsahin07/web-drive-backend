namespace Drive.BuildingBlocks.Infrastructure.Cosmos
{
    public sealed class CosmosOptions
    {
        public const string SectionName = "Cosmos";

        public string? Endpoint { get; init; }

        public string? AccountKey { get; init; }

        public string DatabaseName { get; init; } = "drive";

        public bool UseGatewayMode { get; init; }
    }
}
