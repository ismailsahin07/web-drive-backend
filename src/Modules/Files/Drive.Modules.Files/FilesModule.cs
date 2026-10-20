using Drive.BuildingBlocks.Infrastructure.Cosmos;
using Drive.Modules.Files.Application.Abstractions;
using Drive.Modules.Files.Application.Access;
using Drive.Modules.Files.Application.Downloads.GetDownloadUrl;
using Drive.Modules.Files.Application.Drives;
using Drive.Modules.Files.Application.Drives.GetOrCreateMyDrive;
using Drive.Modules.Files.Application.Folders.CreateFolder;
using Drive.Modules.Files.Application.Items;
using Drive.Modules.Files.Application.Items.GetBreadcrumbs;
using Drive.Modules.Files.Application.Items.GetItem;
using Drive.Modules.Files.Application.Items.ListChildren;
using Drive.Modules.Files.Application.Items.ListTrash;
using Drive.Modules.Files.Application.Items.MoveItem;
using Drive.Modules.Files.Application.Items.RenameItem;
using Drive.Modules.Files.Application.Items.RestoreItem;
using Drive.Modules.Files.Application.Items.TrashItem;
using Drive.Modules.Files.Application.Uploads.CompleteUpload;
using Drive.Modules.Files.Application.Uploads.InitiateUpload;
using Drive.Modules.Files.Contracts;
using Drive.Modules.Files.Infrastructure.Persistence;
using Drive.SharedKernel.Messaging;
using Drive.SharedKernel.Paging;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Drive.Modules.Files
{
    public static class FilesModule
    {
        public static IServiceCollection AddFilesModule(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<FilesOptions>()
                .Bind(configuration.GetSection(FilesOptions.SectionName))
                .Validate(
                    options => options.DefaultQuotaBytes > 0
                        && options.MaxFileSizeBytes > 0
                        && options.UploadUrlLifetime > TimeSpan.Zero
                        && options.DownloadUrlLifetime > TimeSpan.Zero,
                    "All Files options must be positive.")
                .ValidateOnStart();

            services.AddSingleton(provider => provider.GetRequiredService<IOptions<FilesOptions>>().Value);
            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<DriveAccess>();
            services.AddScoped<ItemTree>();
            services.AddScoped<AvailableNameFinder>();

            services.AddScoped<ICommandHandler<GetOrCreateMyDriveCommand, StorageDriveDto>, GetOrCreateMyDriveHandler>();
            services.AddScoped<ICommandHandler<CreateFolderCommand, ItemId>, CreateFolderHandler>();
            services.AddScoped<ICommandHandler<InitiateUploadCommand, UploadTicket>, InitiateUploadHandler>();
            services.AddScoped<ICommandHandler<CompleteUploadCommand>, CompleteUploadHandler>();
            services.AddScoped<ICommandHandler<RenameItemCommand>, RenameItemHandler>();
            services.AddScoped<ICommandHandler<MoveItemCommand>, MoveItemHandler>();
            services.AddScoped<ICommandHandler<TrashItemCommand>, TrashItemHandler>();
            services.AddScoped<ICommandHandler<RestoreItemCommand>, RestoreItemHandler>();

            services.AddScoped<IQueryHandler<GetItemQuery, DriveItemDto>, GetItemHandler>();
            services.AddScoped<IQueryHandler<GetDownloadUrlQuery, DownloadLink>, GetDownloadUrlHandler>();
            services.AddScoped<IQueryHandler<ListChildrenQuery, PagedResult<DriveItemDto>>, ListChildrenHandler>();
            services.AddScoped<IQueryHandler<ListTrashQuery, PagedResult<DriveItemDto>>, ListTrashHandler>();
            services.AddScoped<IQueryHandler<GetBreadcrumbsQuery, IReadOnlyList<BreadcrumbDto>>, GetBreadcrumbsHandler>();

            return services;
        }

        public static IServiceCollection AddFilesInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddCosmos(configuration);

            services.AddScoped(provider =>
            {
                var client = provider.GetRequiredService<CosmosClient>();
                var cosmosOptions = provider.GetRequiredService<IOptions<CosmosOptions>>().Value;
                return new CosmosFilesStore(client.GetContainer(cosmosOptions.DatabaseName, FilesCosmosSchema.ContainerName));
            });

            services.AddScoped<IStorageDriveRepository>(provider => provider.GetRequiredService<CosmosFilesStore>());
            services.AddScoped<IDriveItemRepository>(provider => provider.GetRequiredService<CosmosFilesStore>());
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CosmosFilesStore>());

            return services;
        }
    }
}
