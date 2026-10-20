using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Drive.BuildingBlocks.Infrastructure.Cosmos
{
    public static class CosmosServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmos(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<CosmosOptions>()
                .Bind(configuration.GetSection(CosmosOptions.SectionName))
                .Validate(
                    options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _)
                        && !string.IsNullOrWhiteSpace(options.DatabaseName),
                    "Cosmos:Endpoint must be an absolute URL and Cosmos:DatabaseName must be set.")
                .ValidateOnStart();

            services.TryAddSingleton<CosmosClient>(
                provider => CosmosClientFactory.Create(provider.GetRequiredService<IOptions<CosmosOptions>>().Value));

            return services;
        }
    }
}
