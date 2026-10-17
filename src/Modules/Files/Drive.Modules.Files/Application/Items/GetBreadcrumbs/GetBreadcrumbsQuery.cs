using Drive.Modules.Files.Contracts;
using Drive.SharedKernel.Messaging;

namespace Drive.Modules.Files.Application.Items.GetBreadcrumbs
{
    public sealed record GetBreadcrumbsQuery(DriveId DriveId, ItemId ItemId) : IQuery<IReadOnlyList<BreadcrumbDto>>;
}
