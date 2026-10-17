using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.UnitTests.TestDoubles;
using Drive.SharedKernel.Abstractions;
using Drive.SharedKernel.Domain;
using Drive.SharedKernel.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Drive.Modules.Files.UnitTests
{
    public sealed class FilesModuleTests
    {
        private static readonly Type[] HandlerInterfaces =
        [
            typeof(ICommandHandler<>),
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>),
        ];

        [Fact]
        public void AddFilesModule_RegistersEveryHandlerInTheModule()
        {
            using var provider = BuildProvider(new Dictionary<string, string?>());
            using var scope = provider.CreateScope();

            var handlers = typeof(FilesModule).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false })
                .SelectMany(type => type.GetInterfaces()
                    .Where(contract => contract.IsGenericType && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
                    .Select(contract => (Contract: contract, Implementation: type)))
                .ToList();

            handlers.ShouldNotBeEmpty();
            foreach (var (contract, implementation) in handlers)
            {
                scope.ServiceProvider.GetRequiredService(contract).ShouldBeOfType(implementation);
            }
        }

        [Fact]
        public void AddFilesModule_BindsOptionsFromConfiguration()
        {
            using var provider = BuildProvider(new Dictionary<string, string?>
            {
                ["Files:MaxFileSizeBytes"] = "1234",
                ["Files:DownloadUrlLifetime"] = "00:05:00",
            });

            var options = provider.GetRequiredService<FilesOptions>();

            options.MaxFileSizeBytes.ShouldBe(1234);
            options.DownloadUrlLifetime.ShouldBe(TimeSpan.FromMinutes(5));
            options.DefaultQuotaBytes.ShouldBe(new FilesOptions().DefaultQuotaBytes);
        }

        [Fact]
        public void AddFilesModule_InvalidOptions_FailValidation()
        {
            using var provider = BuildProvider(new Dictionary<string, string?>
            {
                ["Files:MaxFileSizeBytes"] = "0",
            });

            Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<FilesOptions>());
        }

        [Fact]
        public void AddFilesModule_RegistersSystemClockByDefault()
        {
            using var provider = BuildProvider(new Dictionary<string, string?>());

            provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
        }

        private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var store = new InMemoryFilesStore();

            var services = new ServiceCollection();
            services.AddFilesModule(configuration);

            services.AddSingleton<IStorageDriveRepository>(store);
            services.AddSingleton<IDriveItemRepository>(store);
            services.AddSingleton<IUnitOfWork>(store);
            services.AddSingleton<IBlobStorage, FakeBlobStorage>();
            services.AddSingleton<ICurrentUser>(new FakeCurrentUser(UserId.From(Guid.NewGuid())));

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        }
    }
}
